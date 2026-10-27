using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class SoundManagerTests
    {
        private GameObject _parent;
        private FakeAssetLoader _loader;
        private SoundManager _sound;
        private readonly List<Object> _clips = new();

        [SetUp]
        public void SetUp()
        {
            _parent = new GameObject("Sound Parent");
            _loader = new FakeAssetLoader { Factory = path => path.Contains("Missing") ? null : CreateClip(path) };
            _sound = new SoundManager(null, _parent.transform, _loader);
        }

        [TearDown]
        public void TearDown()
        {
            _sound.Dispose();
            Object.DestroyImmediate(_parent);
            foreach (var clip in _clips)
                Object.DestroyImmediate(clip);
            _clips.Clear();
        }

        // 1초짜리 무음 클립입니다.
        private AudioClip CreateClip(string name)
        {
            var clip = AudioClip.Create(name, 44100, 1, 44100, false);
            _clips.Add(clip);
            return clip;
        }

        // 가짜 로더는 즉시 완료되므로 재생 함수도 동기적으로 끝납니다.
        private void Run(Awaitable awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "재생 함수가 동기적으로 완료되지 않았습니다.");
            awaiter.GetResult();
        }

        private int LoadCount(string path) => _loader.Log.Count(entry => entry.StartsWith($"load:{path}:"));

        [Test]
        public void DefaultSettings_HaveEightSfxChannels()
        {
            Assert.AreEqual(8, _sound.SfxChannelCount);
        }

        [Test]
        public void PlaySFX_LoadsFromSfxPathWithAddressables()
        {
            Run(_sound.PlaySFXAsync("Hit"));

            Assert.AreEqual("load:SFX/Hit:Addressables", _loader.Log[0]);
            Assert.AreEqual("SFX/Hit", _sound.GetSfxSource(0).clip.name);
        }

        [Test]
        public void PlaySFX_VolumeIsMasterTimesSfxTimesClipVolume()
        {
            _sound.MasterVolume = 0.5f;
            _sound.SfxVolume = 0.5f;

            Run(_sound.PlaySFXAsync("Hit", 0.5f));

            Assert.AreEqual(0.125f, _sound.GetSfxSource(0).volume, 1e-5f);
        }

        [Test]
        public void ChangingVolumes_UpdatesPlayingSounds()
        {
            Run(_sound.PlaySFXAsync("Hit", 0.5f));
            Run(_sound.PlayBGMAsync("Title", 0.8f));

            _sound.MasterVolume = 0.5f;
            Assert.AreEqual(0.25f, _sound.GetSfxSource(0).volume, 1e-5f);
            Assert.AreEqual(0.4f, _sound.ActiveBgmSource.volume, 1e-5f);

            _sound.BgmVolume = 0.5f;
            Assert.AreEqual(0.2f, _sound.ActiveBgmSource.volume, 1e-5f);
            Assert.AreEqual(0.25f, _sound.GetSfxSource(0).volume, 1e-5f);
        }

        [Test]
        public void Volume_IsClampedAndRaisesChangedOnlyWhenValueChanges()
        {
            var changed = 0;
            _sound.VolumeChanged += () => changed++;

            _sound.MasterVolume = 2f;
            Assert.AreEqual(1f, _sound.MasterVolume);
            Assert.AreEqual(0, changed, "이미 1이므로 바뀌지 않아야 합니다.");

            _sound.MasterVolume = 0.3f;
            _sound.MasterVolume = 0.3f;
            _sound.MasterVolume = -1f;

            Assert.AreEqual(0f, _sound.MasterVolume);
            Assert.AreEqual(2, changed);
        }

        [Test]
        public void PlaySFXAndPlayBGM_WithoutAwait_StartPlaying()
        {
            _sound.PlaySFX("Hit");
            _sound.PlayBGM("Title");

            Assert.AreEqual("SFX/Hit", _sound.GetSfxSource(0).clip.name);
            Assert.AreEqual("Title", _sound.CurrentBgm);

            _sound.StopBGM();
            Assert.IsNull(_sound.CurrentBgm);
        }

        [Test]
        public void PlaySFX_WithoutAwait_Missing_LogsErrorWithoutThrowing()
        {
            LogAssert.Expect(LogType.Error, new Regex("Missing"));

            _sound.PlaySFX("Missing");

            Assert.IsNull(_sound.GetSfxSource(0).clip);
        }

        [Test]
        public void PlaySFX_SameName_LoadsOnce()
        {
            Run(_sound.PlaySFXAsync("Hit"));
            Run(_sound.PlaySFXAsync("Hit"));

            Assert.AreEqual(1, LoadCount("SFX/Hit"));
        }

        [Test]
        public void PlaySFX_WhenAllChannelsBusy_ReplacesOldest()
        {
            for (var i = 0; i < 8; i++)
                Run(_sound.PlaySFXAsync($"S{i}"));

            Run(_sound.PlaySFXAsync("S8"));

            Assert.AreEqual("SFX/S8", _sound.GetSfxSource(0).clip.name);
            Assert.AreEqual("SFX/S1", _sound.GetSfxSource(1).clip.name);
            Assert.AreEqual("SFX/S7", _sound.GetSfxSource(7).clip.name);
        }

        [Test]
        public void PlaySFX_Missing_LogsErrorWithoutThrowing()
        {
            LogAssert.Expect(LogType.Error, new Regex("Missing"));

            Run(_sound.PlaySFXAsync("Missing"));

            Assert.IsNull(_sound.GetSfxSource(0).clip);
        }

        [Test]
        public void PlayBGM_LoadsFromBgmPathAndLoops()
        {
            Run(_sound.PlayBGMAsync("Title"));

            CollectionAssert.Contains(_loader.Log, "load:BGM/Title:Addressables");
            Assert.AreEqual("Title", _sound.CurrentBgm);
            Assert.AreEqual("BGM/Title", _sound.ActiveBgmSource.clip.name);
            Assert.IsTrue(_sound.ActiveBgmSource.loop);
        }

        [Test]
        public void PlayBGM_Switch_ReleasesPreviousBgm()
        {
            Run(_sound.PlayBGMAsync("A"));
            Run(_sound.PlayBGMAsync("B"));

            Assert.AreEqual("B", _sound.CurrentBgm);
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
            CollectionAssert.DoesNotContain(_loader.Log, "release:BGM/B:Addressables");
        }

        [Test]
        public void PlayBGM_SameName_DoesNotReloadAndUpdatesVolume()
        {
            Run(_sound.PlayBGMAsync("A"));
            Run(_sound.PlayBGMAsync("A", 0.5f));

            Assert.AreEqual(1, LoadCount("BGM/A"));
            Assert.AreEqual(0.5f, _sound.ActiveBgmSource.volume, 1e-5f);
        }

        [Test]
        public void StopBGM_StopsAndReleases()
        {
            Run(_sound.PlayBGMAsync("A"));

            Run(_sound.StopBGMAsync());

            Assert.IsNull(_sound.CurrentBgm);
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
        }

        [Test]
        public void UnloadSFX_ReleasesAndLoadsAgainNextTime()
        {
            Run(_sound.PlaySFXAsync("Hit"));

            _sound.UnloadSFX("Hit");
            Run(_sound.PlaySFXAsync("Hit"));

            CollectionAssert.Contains(_loader.Log, "release:SFX/Hit:Addressables");
            Assert.AreEqual(2, LoadCount("SFX/Hit"));
        }

        [Test]
        public void Volumes_AreLoadedFromSaveOnCreate()
        {
            var save = new SaveService(new MemorySaveStorage());
            save.Save(SoundManager.VolumeSaveKey, new SoundVolumeData { master = 0.2f, bgm = 0.3f, sfx = 0.4f }, SaveScope.Global);

            var sound = new SoundManager(null, _parent.transform, _loader, save);

            Assert.AreEqual(0.2f, sound.MasterVolume, 1e-5f);
            Assert.AreEqual(0.3f, sound.BgmVolume, 1e-5f);
            Assert.AreEqual(0.4f, sound.SfxVolume, 1e-5f);
            sound.Dispose();
        }

        [Test]
        public void Volumes_AreSavedOnDisposeAfterChange()
        {
            var save = new SaveService(new MemorySaveStorage());
            var sound = new SoundManager(null, _parent.transform, _loader, save);

            sound.SfxVolume = 0.25f;
            sound.Dispose();

            var saved = save.Load<SoundVolumeData>(SoundManager.VolumeSaveKey, scope: SaveScope.Global);
            Assert.AreEqual(0.25f, saved.sfx, 1e-5f);
            Assert.AreEqual(1f, saved.master, 1e-5f);
        }

        [Test]
        public void Volumes_AreSavedWhenFocusIsLost()
        {
            var save = new SaveService(new MemorySaveStorage());
            var sound = new SoundManager(null, _parent.transform, _loader, save);
            sound.BgmVolume = 0.4f;

            sound.OnFocusChanged(false);

            var saved = save.Load<SoundVolumeData>(SoundManager.VolumeSaveKey, scope: SaveScope.Global);
            Assert.AreEqual(0.4f, saved.bgm, 1e-5f);
            sound.Dispose();
        }

        [Test]
        public void Dispose_ReleasesLoadedSounds()
        {
            Run(_sound.PlaySFXAsync("Hit"));
            Run(_sound.PlayBGMAsync("A"));

            _sound.Dispose();

            CollectionAssert.Contains(_loader.Log, "release:SFX/Hit:Addressables");
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
        }
    }
}
