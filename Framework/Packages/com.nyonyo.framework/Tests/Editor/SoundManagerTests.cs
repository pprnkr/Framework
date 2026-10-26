using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nyonyo.Framework.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nyonyo.Framework.Editor.Tests
{
    public class SoundManagerTests
    {
        GameObject _parent;
        FakeAssetLoader _loader;
        SoundManager _sound;
        readonly List<Object> _clips = new();

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
        AudioClip CreateClip(string name)
        {
            var clip = AudioClip.Create(name, 44100, 1, 44100, false);
            _clips.Add(clip);
            return clip;
        }

        // 가짜 로더는 즉시 완료되므로 재생 함수도 동기적으로 끝납니다.
        static void Run(Awaitable awaitable)
        {
            var awaiter = awaitable.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted, "재생 함수가 동기적으로 완료되지 않았습니다.");
            awaiter.GetResult();
        }

        int LoadCount(string path) => _loader.Log.Count(entry => entry.StartsWith($"load:{path}:"));

        [Test]
        public void DefaultSettings_HaveEightSfxChannels()
        {
            Assert.AreEqual(8, _sound.SfxChannelCount);
        }

        [Test]
        public void PlaySFX_LoadsFromSfxPathWithAddressables()
        {
            Run(_sound.PlaySFX("Hit"));

            Assert.AreEqual("load:SFX/Hit:Addressables", _loader.Log[0]);
            Assert.AreEqual("SFX/Hit", _sound.GetSfxSource(0).clip.name);
        }

        [Test]
        public void PlaySFX_VolumeIsMasterTimesSfxTimesClipVolume()
        {
            _sound.MasterVolume = 0.5f;
            _sound.SfxVolume = 0.5f;

            Run(_sound.PlaySFX("Hit", 0.5f));

            Assert.AreEqual(0.125f, _sound.GetSfxSource(0).volume, 1e-5f);
        }

        [Test]
        public void ChangingVolumes_UpdatesPlayingSounds()
        {
            Run(_sound.PlaySFX("Hit", 0.5f));
            Run(_sound.PlayBGM("Title", 0.8f));

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
        public void PlaySFX_SameName_LoadsOnce()
        {
            Run(_sound.PlaySFX("Hit"));
            Run(_sound.PlaySFX("Hit"));

            Assert.AreEqual(1, LoadCount("SFX/Hit"));
        }

        [Test]
        public void PlaySFX_WhenAllChannelsBusy_ReplacesOldest()
        {
            for (var i = 0; i < 8; i++)
                Run(_sound.PlaySFX($"S{i}"));

            Run(_sound.PlaySFX("S8"));

            Assert.AreEqual("SFX/S8", _sound.GetSfxSource(0).clip.name);
            Assert.AreEqual("SFX/S1", _sound.GetSfxSource(1).clip.name);
            Assert.AreEqual("SFX/S7", _sound.GetSfxSource(7).clip.name);
        }

        [Test]
        public void PlaySFX_Missing_LogsErrorWithoutThrowing()
        {
            LogAssert.Expect(LogType.Error, new Regex("Missing"));

            Run(_sound.PlaySFX("Missing"));

            Assert.IsNull(_sound.GetSfxSource(0).clip);
        }

        [Test]
        public void PlayBGM_LoadsFromBgmPathAndLoops()
        {
            Run(_sound.PlayBGM("Title"));

            CollectionAssert.Contains(_loader.Log, "load:BGM/Title:Addressables");
            Assert.AreEqual("Title", _sound.CurrentBgm);
            Assert.AreEqual("BGM/Title", _sound.ActiveBgmSource.clip.name);
            Assert.IsTrue(_sound.ActiveBgmSource.loop);
        }

        [Test]
        public void PlayBGM_Switch_ReleasesPreviousBgm()
        {
            Run(_sound.PlayBGM("A"));
            Run(_sound.PlayBGM("B"));

            Assert.AreEqual("B", _sound.CurrentBgm);
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
            CollectionAssert.DoesNotContain(_loader.Log, "release:BGM/B:Addressables");
        }

        [Test]
        public void PlayBGM_SameName_DoesNotReloadAndUpdatesVolume()
        {
            Run(_sound.PlayBGM("A"));
            Run(_sound.PlayBGM("A", 0.5f));

            Assert.AreEqual(1, LoadCount("BGM/A"));
            Assert.AreEqual(0.5f, _sound.ActiveBgmSource.volume, 1e-5f);
        }

        [Test]
        public void StopBGM_StopsAndReleases()
        {
            Run(_sound.PlayBGM("A"));

            Run(_sound.StopBGM());

            Assert.IsNull(_sound.CurrentBgm);
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
        }

        [Test]
        public void UnloadSFX_ReleasesAndLoadsAgainNextTime()
        {
            Run(_sound.PlaySFX("Hit"));

            _sound.UnloadSFX("Hit");
            Run(_sound.PlaySFX("Hit"));

            CollectionAssert.Contains(_loader.Log, "release:SFX/Hit:Addressables");
            Assert.AreEqual(2, LoadCount("SFX/Hit"));
        }

        [Test]
        public void Dispose_ReleasesLoadedSounds()
        {
            Run(_sound.PlaySFX("Hit"));
            Run(_sound.PlayBGM("A"));

            _sound.Dispose();

            CollectionAssert.Contains(_loader.Log, "release:SFX/Hit:Addressables");
            CollectionAssert.Contains(_loader.Log, "release:BGM/A:Addressables");
        }
    }
}
