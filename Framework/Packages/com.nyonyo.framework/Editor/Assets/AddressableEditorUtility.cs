using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Assets
{
    /// <summary>
    /// 에셋을 Addressables에 등록합니다.
    /// </summary>
    public static class AddressableEditorUtility
    {
        /// <summary>
        /// 에셋을 Addressables에 등록하고 address를 지정합니다. 이미 등록돼 있으면 address만 바꿉니다.
        /// 프로젝트에 Addressables 설정이 없으면 기본 설정을 만듭니다.
        /// </summary>
        /// <returns>등록했으면 true, 에셋을 찾지 못했으면 false</returns>
        public static bool SetAddress(string assetPath, string address)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[Nyonyo Framework] {assetPath} 에셋을 찾지 못해서 Addressables에 등록하지 못했습니다.");
                return false;
            }

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var entry = settings.FindAssetEntry(guid) ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            if (entry.address != address)
                entry.SetAddress(address);

            return true;
        }
    }
}
