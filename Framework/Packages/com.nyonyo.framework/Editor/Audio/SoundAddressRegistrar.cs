using System.Collections.Generic;
using System.IO;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Editor.Assets;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Audio
{
    /// <summary>
    /// 메뉴 Tools > Nyonyo Framework > Sound > Register Sounds to Addressables.
    /// <see cref="SoundSettings"/>의 BGM, 효과음 폴더에 있는 AudioClip을 이름 규칙대로 Addressables에 등록합니다.
    /// 예: Assets/Sounds/BGM/Title.wav는 address BGM/Title로 등록되어 <c>PlayBGM("Title")</c>로 재생할 수 있습니다.
    /// </summary>
    public static class SoundAddressRegistrar
    {
        [MenuItem("Tools/Nyonyo Framework/Sound/Register Sounds to Addressables")]
        static void RegisterFromMenu()
        {
            var settings = FindSettings();
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Register Sounds",
                    "SoundSettings 에셋이 없습니다. Create > Nyonyo Framework > Sound Settings로 만든 뒤 다시 실행해 주세요.", "확인");
                return;
            }

            if (settings.LoadType != LoadType.Addressables)
            {
                EditorUtility.DisplayDialog("Register Sounds",
                    $"{settings.name}의 Load Type이 Resources라서 등록할 필요가 없습니다. 사운드를 Resources 폴더에 두세요.", "확인");
                return;
            }

            Register(settings);
        }

        /// <summary>
        /// <paramref name="settings"/>의 BGM, 효과음 폴더에 있는 AudioClip을 Addressables에 등록합니다.
        /// </summary>
        /// <returns>등록한 사운드 수</returns>
        public static int Register(SoundSettings settings)
        {
            var count = Register(settings.BgmFolder, settings.BgmPathFormat) + Register(settings.SfxFolder, settings.SfxPathFormat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nyonyo Framework] 사운드 {count}개를 Addressables에 등록했습니다.");
            return count;
        }

        static int Register(string folder, string pathFormat)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"[Nyonyo Framework] 사운드 폴더 {folder}이(가) 없어서 건너뜁니다.");
                return 0;
            }

            var count = 0;
            var names = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var soundName = Path.GetFileNameWithoutExtension(assetPath);
                if (!names.Add(soundName))
                {
                    Debug.LogWarning($"[Nyonyo Framework] {folder}에 이름이 {soundName}인 사운드가 여러 개 있어서 {assetPath}을(를) 건너뜁니다.");
                    continue;
                }

                if (AddressableEditorUtility.SetAddress(assetPath, string.Format(pathFormat, soundName)))
                    count++;
            }

            return count;
        }

        static SoundSettings FindSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(SoundSettings)}"))
            {
                var settings = AssetDatabase.LoadAssetAtPath<SoundSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null)
                    return settings;
            }

            return null;
        }
    }
}
