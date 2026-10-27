using System;
using System.Collections.Generic;
using System.Threading;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Save;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Nyonyo.Framework.Audio
{
    /// <summary>
    /// BGM과 효과음을 재생합니다. 사운드는 이름으로 부르고, <see cref="SoundSettings"/>의 경로 규칙으로 찾아서 불러옵니다.
    /// </summary>
    /// <remarks>
    /// 실제 볼륨은 0~1 값의 곱입니다.
    /// BGM은 MasterVolume × BgmVolume × 재생할 때 넘긴 volume이고, 효과음은 MasterVolume × SfxVolume × 재생할 때 넘긴 volume입니다.
    /// 효과음은 기본 8개까지 동시에 들리고, 다 차면 가장 오래된 효과음을 끊고 재생합니다.
    /// 효과음은 한 번 불러오면 <see cref="UnloadSFX"/>를 부를 때까지 메모리에 남고, BGM은 다른 BGM으로 바뀌거나 멈출 때 내려갑니다.
    /// <see cref="SaveService"/>를 넘기면 볼륨을 전역 데이터로 저장하고, 다음 실행 때 불러옵니다.
    /// </remarks>
    public sealed class SoundManager : IDisposable
    {
        /// <summary>볼륨을 저장하는 전역 저장 키입니다.</summary>
        public const string VolumeSaveKey = "sound.volume";

        // 슬라이더를 움직이는 동안 매번 저장하지 않도록, 마지막 변경 뒤 이 시간(실시간 초)이 지나면 저장합니다.
        private const double VolumeSaveDelay = 0.5;

        private sealed class BgmChannel
        {
            public AudioSource Source;
            public string Name;
            public string Path;
            public float Volume = 1f;
            public float Fade;
        }

        private sealed class SfxChannel
        {
            public AudioSource Source;
            public string Name;
            public float Volume = 1f;
            public double StartTime;
            public double EndTime;
        }

        private readonly IAssetLoader _loader;
        private readonly bool _ownsLoader;
        private readonly LoadType _loadType;
        private readonly string _bgmPathFormat;
        private readonly string _sfxPathFormat;
        private readonly GameObject _root;
        private readonly BgmChannel[] _bgm = new BgmChannel[2];
        private readonly SfxChannel[] _sfx;
        private readonly Dictionary<string, AudioClip> _sfxClips = new();
        private readonly CancellationTokenSource _disposeCts = new();
        private readonly SaveService _save;
        private CancellationTokenSource _bgmCts;
        private int _activeBgm;
        private int _volumeSaveRequest;
        private bool _volumeDirty;
        private float _masterVolume = 1f;
        private float _bgmVolume = 1f;
        private float _sfxVolume = 1f;

        /// <summary>
        /// 서비스로 등록된 SoundManager입니다. <c>Services.Get&lt;SoundManager&gt;()</c>와 같습니다.
        /// </summary>
        public static SoundManager Instance => Services.Get<SoundManager>();

        /// <param name="settings">경로 규칙과 채널 수입니다. null이면 기본값(Addressables, BGM/{0}, SFX/{0}, 8채널)을 씁니다.</param>
        /// <param name="parent">AudioSource를 붙일 부모입니다. 보통 <see cref="Bootstrapper.Root"/>를 넘깁니다. null이면 DontDestroyOnLoad 오브젝트로 만듭니다.</param>
        /// <param name="loader">사운드를 불러올 로더입니다. 서비스로 등록한 <see cref="AssetLoader"/>를 넘기세요. null이면 직접 하나 만들어 씁니다.</param>
        /// <param name="save">넘기면 저장된 볼륨을 불러오고, 볼륨이 바뀌면 자동으로 저장합니다.</param>
        public SoundManager(SoundSettings settings, Transform parent, IAssetLoader loader = null, SaveService save = null)
        {
            _save = save;
            _ownsLoader = loader == null;
            _loader = loader ?? new AssetLoader();
            _loadType = settings != null ? settings.LoadType : LoadType.Addressables;
            _bgmPathFormat = settings != null ? settings.BgmPathFormat : SoundSettings.DefaultBgmPathFormat;
            _sfxPathFormat = settings != null ? settings.SfxPathFormat : SoundSettings.DefaultSfxPathFormat;
            var sfxChannels = Mathf.Max(1, settings != null ? settings.SfxChannels : SoundSettings.DefaultSfxChannels);

            _root = new GameObject("[Sound]");
            if (parent != null)
                _root.transform.SetParent(parent, false);
            else if (Application.isPlaying)
                Object.DontDestroyOnLoad(_root);

            for (var i = 0; i < _bgm.Length; i++)
                _bgm[i] = new BgmChannel { Source = CreateSource($"BGM {i}") };

            _sfx = new SfxChannel[sfxChannels];
            for (var i = 0; i < _sfx.Length; i++)
                _sfx[i] = new SfxChannel { Source = CreateSource($"SFX {i}") };

            LoadVolumes();

            // Android, iOS, WebGL은 종료 이벤트 없이 앱이 내려가거나 탭이 닫힐 수 있습니다.
            // 그래서 포커스를 잃는 순간(백그라운드 전환, 탭 전환) 저장하지 않은 볼륨을 바로 저장합니다.
            Application.focusChanged += OnFocusChanged;
        }

        /// <summary>
        /// 볼륨 값이 바뀌면 호출됩니다. 설정 화면의 슬라이더를 맞출 때 씁니다.
        /// </summary>
        public event Action VolumeChanged;

        /// <summary>전체 볼륨(0~1)입니다. BGM과 효과음 모두에 곱해집니다.</summary>
        public float MasterVolume
        {
            get => _masterVolume;
            set => SetVolume(ref _masterVolume, value);
        }

        /// <summary>BGM 볼륨(0~1)입니다.</summary>
        public float BgmVolume
        {
            get => _bgmVolume;
            set => SetVolume(ref _bgmVolume, value);
        }

        /// <summary>효과음 볼륨(0~1)입니다.</summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set => SetVolume(ref _sfxVolume, value);
        }

        /// <summary>지금 재생 중인 BGM 이름입니다. 없으면 null입니다.</summary>
        public string CurrentBgm => _bgm[_activeBgm].Name;

        /// <summary>동시에 들릴 수 있는 효과음 수입니다.</summary>
        public int SfxChannelCount => _sfx.Length;

        /// <summary>
        /// BGM을 재생합니다. 같은 BGM이 이미 재생 중이면 진행 중인 페이드를 멈추고 볼륨만 바꿉니다.
        /// 불러오기와 크로스페이드는 내부에서 진행되므로 기다릴 필요가 없습니다. 끝날 때까지 기다려야 하면 <see cref="PlayBGMAsync"/>를 쓰세요.
        /// </summary>
        /// <param name="name">사운드 이름입니다. 경로는 BGM 경로 형식(기본 BGM/{0})으로 만듭니다.</param>
        /// <param name="volume">이 BGM의 볼륨(0~1)입니다.</param>
        /// <param name="fadeSeconds">0보다 크면 이전 BGM과 이 시간 동안 크로스페이드합니다.</param>
        /// <param name="loop">반복 재생 여부입니다.</param>
        public void PlayBGM(string name, float volume = 1f, float fadeSeconds = 0f, bool loop = true)
            => PlayBGMAsync(name, volume, fadeSeconds, loop).Forget();

        /// <summary>
        /// <see cref="PlayBGM"/>과 같고, 불러오기와 크로스페이드가 끝날 때까지 기다릴 수 있습니다.
        /// </summary>
        public async Awaitable PlayBGMAsync(string name, float volume = 1f, float fadeSeconds = 0f, bool loop = true)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[Nyonyo Framework] BGM 이름이 비어 있습니다.");
                return;
            }

            var current = _bgm[_activeBgm];
            if (current.Name == name)
            {
                // 페이드아웃 중이었다면 멈추고 다시 원래 볼륨으로 돌립니다.
                RestartBgmToken();
                StopChannel(_bgm[1 - _activeBgm]);
                current.Fade = 1f;
                current.Volume = Mathf.Clamp01(volume);
                current.Source.loop = loop;
                ApplyVolumes();
                return;
            }

            var token = RestartBgmToken();
            var path = string.Format(_bgmPathFormat, name);
            AudioClip clip;
            try
            {
                clip = await _loader.LoadAsync<AudioClip>(path, _loadType, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Nyonyo Framework] BGM {name}을(를) 불러오지 못했습니다. {e.Message}");
                return;
            }

            // 불러오는 동안 더 새로운 요청이 들어왔다면, 그 요청에 맡기고 이번 것은 버립니다.
            if (token.IsCancellationRequested)
            {
                _loader.Release(path, _loadType);
                return;
            }

            var previous = _bgm[_activeBgm];
            _activeBgm = 1 - _activeBgm;
            var next = _bgm[_activeBgm];
            StopChannel(next);

            next.Name = name;
            next.Path = path;
            next.Volume = Mathf.Clamp01(volume);
            next.Fade = fadeSeconds > 0f ? 0f : 1f;
            next.Source.clip = clip;
            next.Source.loop = loop;
            next.Source.Play();

            if (fadeSeconds <= 0f)
            {
                StopChannel(previous);
                ApplyVolumes();
                return;
            }

            await CrossFadeAsync(previous, next, fadeSeconds, token);
        }

        /// <summary>
        /// BGM을 멈추고 메모리에서 내립니다. 페이드아웃은 내부에서 진행됩니다.
        /// </summary>
        /// <param name="fadeSeconds">0보다 크면 이 시간 동안 소리를 줄인 뒤 멈춥니다.</param>
        public void StopBGM(float fadeSeconds = 0f) => StopBGMAsync(fadeSeconds).Forget();

        /// <summary>
        /// <see cref="StopBGM"/>과 같고, 페이드아웃이 끝날 때까지 기다릴 수 있습니다.
        /// </summary>
        public async Awaitable StopBGMAsync(float fadeSeconds = 0f)
        {
            var token = RestartBgmToken();
            StopChannel(_bgm[1 - _activeBgm]);

            var active = _bgm[_activeBgm];
            if (active.Name == null)
                return;

            if (fadeSeconds > 0f)
            {
                var startFade = active.Fade;
                try
                {
                    for (var elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
                    {
                        active.Fade = startFade * (1f - elapsed / fadeSeconds);
                        ApplyVolumes();
                        await Awaitable.NextFrameAsync(token);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            StopChannel(active);
        }

        /// <summary>
        /// 효과음을 재생합니다. 이미 불러온 효과음은 바로 재생하고, 처음 재생하는 효과음은 내부에서 불러온 뒤에 재생합니다.
        /// </summary>
        /// <param name="name">사운드 이름입니다. 경로는 효과음 경로 형식(기본 SFX/{0})으로 만듭니다.</param>
        /// <param name="volume">이 효과음의 볼륨(0~1)입니다.</param>
        public void PlaySFX(string name, float volume = 1f) => PlaySFXAsync(name, volume).Forget();

        /// <summary>
        /// <see cref="PlaySFX"/>와 같고, 불러와서 재생을 시작할 때까지 기다릴 수 있습니다.
        /// </summary>
        public async Awaitable PlaySFXAsync(string name, float volume = 1f)
        {
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("[Nyonyo Framework] 효과음 이름이 비어 있습니다.");
                return;
            }

            if (!_sfxClips.TryGetValue(name, out var clip))
            {
                try
                {
                    clip = await LoadSfxAsync(name);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Nyonyo Framework] 효과음 {name}을(를) 불러오지 못했습니다. {e.Message}");
                    return;
                }
            }

            PlayOnChannel(name, clip, volume);
        }

        /// <summary>
        /// 효과음을 미리 불러 둡니다. 처음 재생할 때 생기는 지연을 없앨 때 씁니다.
        /// </summary>
        public async Awaitable PreloadSFXAsync(params string[] names)
        {
            foreach (var name in names)
            {
                if (string.IsNullOrEmpty(name) || _sfxClips.ContainsKey(name))
                    continue;

                try
                {
                    await LoadSfxAsync(name);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Nyonyo Framework] 효과음 {name}을(를) 불러오지 못했습니다. {e.Message}");
                }
            }
        }

        /// <summary>
        /// 재생 중인 효과음을 모두 멈춥니다.
        /// </summary>
        public void StopAllSFX()
        {
            foreach (var channel in _sfx)
                StopChannel(channel);
        }

        /// <summary>
        /// 불러온 효과음을 메모리에서 내립니다. 재생 중이면 멈춥니다.
        /// </summary>
        public void UnloadSFX(string name)
        {
            if (name == null || !_sfxClips.Remove(name))
                return;

            foreach (var channel in _sfx)
            {
                if (channel.Name == name)
                    StopChannel(channel);
            }

            _loader.Release(string.Format(_sfxPathFormat, name), _loadType);
        }

        /// <summary>
        /// 불러온 효과음을 모두 메모리에서 내립니다. 씬을 바꿀 때 씁니다.
        /// </summary>
        public void UnloadAllSFX()
        {
            foreach (var name in new List<string>(_sfxClips.Keys))
                UnloadSFX(name);
        }

        /// <summary>
        /// 지금 볼륨을 바로 저장합니다. 볼륨은 바뀌면 자동으로 저장되므로 보통은 부를 필요가 없습니다.
        /// </summary>
        public void SaveVolumes()
        {
            if (_save == null)
                return;

            _volumeDirty = false;
            var data = new SoundVolumeData { master = _masterVolume, bgm = _bgmVolume, sfx = _sfxVolume };
            _save.Save(VolumeSaveKey, data, SaveScope.Global);
        }

        /// <summary>
        /// 저장하지 않은 볼륨 변경을 저장한 뒤, 모든 사운드를 멈추고 불러온 사운드를 로더에 반환합니다.
        /// </summary>
        public void Dispose()
        {
            Application.focusChanged -= OnFocusChanged;
            if (_volumeDirty)
                SaveVolumes();

            _disposeCts.Cancel();
            _bgmCts?.Cancel();
            foreach (var channel in _bgm)
                StopChannel(channel);
            UnloadAllSFX();

            if (_ownsLoader && _loader is IDisposable disposable)
                disposable.Dispose();
        }

        internal AudioSource GetSfxSource(int index) => _sfx[index].Source;

        internal void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus && _volumeDirty)
                SaveVolumes();
        }

        internal AudioSource ActiveBgmSource => _bgm[_activeBgm].Source;

        private async Awaitable<AudioClip> LoadSfxAsync(string name)
        {
            var path = string.Format(_sfxPathFormat, name);
            var clip = await _loader.LoadAsync<AudioClip>(path, _loadType, _disposeCts.Token);

            // 같은 효과음을 동시에 불러온 경우, 먼저 끝난 쪽만 남기고 나머지는 반환합니다.
            if (_sfxClips.TryGetValue(name, out var existing))
            {
                _loader.Release(path, _loadType);
                return existing;
            }

            _sfxClips.Add(name, clip);
            return clip;
        }

        private void PlayOnChannel(string name, AudioClip clip, float volume)
        {
            var now = Time.realtimeSinceStartupAsDouble;
            var channel = FindChannel(now);

            channel.Source.Stop();
            channel.Source.clip = clip;
            channel.Name = name;
            channel.Volume = Mathf.Clamp01(volume);
            channel.StartTime = now;
            channel.EndTime = now + clip.length / Mathf.Max(0.01f, Mathf.Abs(channel.Source.pitch));
            channel.Source.volume = _masterVolume * _sfxVolume * channel.Volume;
            channel.Source.Play();
        }

        // 비어 있는 채널을 고르고, 다 차 있으면 가장 먼저 시작한 효과음의 채널을 씁니다.
        private SfxChannel FindChannel(double now)
        {
            SfxChannel oldest = _sfx[0];
            foreach (var channel in _sfx)
            {
                if (channel.EndTime <= now)
                    return channel;

                if (channel.StartTime < oldest.StartTime)
                    oldest = channel;
            }

            return oldest;
        }

        private async Awaitable CrossFadeAsync(BgmChannel from, BgmChannel to, float seconds, CancellationToken token)
        {
            var fromStart = from.Fade;
            try
            {
                for (var elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
                {
                    var t = elapsed / seconds;
                    to.Fade = t;
                    from.Fade = fromStart * (1f - t);
                    ApplyVolumes();
                    await Awaitable.NextFrameAsync(token);
                }
            }
            catch (OperationCanceledException)
            {
                // 더 새로운 BGM 요청이나 StopBGM이 이어서 처리합니다.
                return;
            }

            to.Fade = 1f;
            StopChannel(from);
            ApplyVolumes();
        }

        // 진행 중인 BGM 로드나 페이드를 취소하고 새 토큰을 만듭니다.
        // 이전 토큰을 아직 기다리는 쪽이 있을 수 있어서 Dispose하지 않습니다.
        private CancellationToken RestartBgmToken()
        {
            _bgmCts?.Cancel();
            _bgmCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);
            return _bgmCts.Token;
        }

        private void StopChannel(BgmChannel channel)
        {
            channel.Source.Stop();
            channel.Source.clip = null;
            if (channel.Path != null)
                _loader.Release(channel.Path, _loadType);

            channel.Name = null;
            channel.Path = null;
            channel.Fade = 0f;
        }

        private void StopChannel(SfxChannel channel)
        {
            channel.Source.Stop();
            channel.Source.clip = null;
            channel.Name = null;
            channel.EndTime = 0d;
        }

        private void SetVolume(ref float field, float value)
        {
            value = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, value))
                return;

            field = value;
            ApplyVolumes();
            VolumeChanged?.Invoke();
            ScheduleVolumeSave();
        }

        private void LoadVolumes()
        {
            if (_save == null || !_save.TryLoad(VolumeSaveKey, out SoundVolumeData data, SaveScope.Global))
                return;

            _masterVolume = Mathf.Clamp01(data.master);
            _bgmVolume = Mathf.Clamp01(data.bgm);
            _sfxVolume = Mathf.Clamp01(data.sfx);
            ApplyVolumes();
        }

        private void ScheduleVolumeSave()
        {
            if (_save == null)
                return;

            _volumeDirty = true;
            SaveVolumesLaterAsync(++_volumeSaveRequest).Forget();
        }

        // 일시정지(timeScale 0) 중에도 저장되도록 실시간으로 기다립니다.
        private async Awaitable SaveVolumesLaterAsync(int request)
        {
            var due = Time.realtimeSinceStartupAsDouble + VolumeSaveDelay;
            try
            {
                while (Time.realtimeSinceStartupAsDouble < due)
                    await Awaitable.NextFrameAsync(_disposeCts.Token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (request == _volumeSaveRequest && _volumeDirty)
                SaveVolumes();
        }

        private void ApplyVolumes()
        {
            foreach (var channel in _bgm)
                channel.Source.volume = _masterVolume * _bgmVolume * channel.Volume * channel.Fade;

            foreach (var channel in _sfx)
                channel.Source.volume = _masterVolume * _sfxVolume * channel.Volume;
        }

        private AudioSource CreateSource(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            return source;
        }
    }
}
