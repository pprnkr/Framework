using Nyonyo.Framework.Assets;
using UnityEngine;

namespace Nyonyo.Framework.Audio
{
    /// <summary>
    /// <see cref="SoundManager"/> 설정입니다. 사운드 이름으로 경로를 만드는 규칙과 효과음 동시 재생 수를 정합니다.
    /// </summary>
    public sealed class SoundSettings : ScriptableObject
    {
        public const string DefaultBgmPathFormat = "BGM/{0}";
        public const string DefaultSfxPathFormat = "SFX/{0}";
        public const int DefaultSfxChannels = 8;

        [Header("Load")]
        [Tooltip("사운드를 불러오는 방식입니다.")]
        [SerializeField] private LoadType _loadType = LoadType.Addressables;

        [Tooltip("BGM 이름으로 경로를 만드는 형식입니다. {0}에 이름이 들어갑니다. " +
                 "Addressables면 address, Resources면 Resources 폴더 기준 경로입니다.")]
        [SerializeField] private string _bgmPathFormat = DefaultBgmPathFormat;

        [Tooltip("효과음 이름으로 경로를 만드는 형식입니다. {0}에 이름이 들어갑니다.")]
        [SerializeField] private string _sfxPathFormat = DefaultSfxPathFormat;

        [Header("Addressables 등록 (에디터)")]
        [Tooltip("Tools > Nyonyo Framework > Sound > Register Sounds to Addressables가 BGM으로 등록할 폴더입니다.")]
        [SerializeField] private string _bgmFolder = "Assets/Sounds/BGM";

        [Tooltip("Tools > Nyonyo Framework > Sound > Register Sounds to Addressables가 효과음으로 등록할 폴더입니다.")]
        [SerializeField] private string _sfxFolder = "Assets/Sounds/SFX";

        [Header("Playback")]
        [Tooltip("동시에 들릴 수 있는 효과음 수입니다. 다 차면 가장 오래된 효과음을 끊고 재생합니다.")]
        [Range(1, 32)]
        [SerializeField] private int _sfxChannels = DefaultSfxChannels;

        public LoadType LoadType => _loadType;

        public string BgmPathFormat => _bgmPathFormat;

        public string SfxPathFormat => _sfxPathFormat;

        public string BgmFolder => _bgmFolder;

        public string SfxFolder => _sfxFolder;

        public int SfxChannels => _sfxChannels;
    }
}
