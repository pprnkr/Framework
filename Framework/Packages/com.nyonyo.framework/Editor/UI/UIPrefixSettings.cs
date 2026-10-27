using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.UI
{
    /// <summary>
    /// UI 자동 매핑 규칙입니다. 게임오브젝트 이름의 접두사(IMG_, TXT_ 등)를 어떤 타입의 변수로 만들지 정합니다.
    /// </summary>
    /// <remarks>
    /// 프로젝트에 하나만 두면 됩니다. 없으면 처음 사용할 때 <see cref="DefaultAssetPath"/>에 기본값으로 만듭니다.
    /// </remarks>
    public sealed class UIPrefixSettings : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/Editor/UIPrefixSettings.asset";

        [Serializable]
        public sealed class Rule
        {
            [Tooltip("게임오브젝트 이름 접두사입니다. 예: IMG_")]
            [SerializeField] private string _prefix;

            [Tooltip("변수 타입의 전체 이름입니다. 예: UnityEngine.UI.Image. UnityEngine.GameObject도 쓸 수 있습니다.")]
            [SerializeField] private string _typeName;

            public Rule()
            {
            }

            public Rule(string prefix, string typeName)
            {
                _prefix = prefix;
                _typeName = typeName;
            }

            public string Prefix => _prefix;

            public string TypeName => _typeName;

            public Type ResolveType() => UITypeResolver.Resolve(_typeName);
        }

        [Tooltip("새로 만드는 스크립트의 namespace입니다. 비워 두면 namespace 없이 만듭니다.")]
        [SerializeField] private string _scriptNamespace = "";

        [Tooltip("Assign Object를 실행할 때 Button의 OnClick에 OnButtonEvent를 자동으로 연결합니다.")]
        [SerializeField] private bool _bindButtonClick = true;

        [Tooltip("접두사가 여러 개 맞으면 가장 긴 접두사를 씁니다.")]
        [SerializeField] private List<Rule> _rules = CreateDefaultRules();

        public string ScriptNamespace => _scriptNamespace == null ? "" : _scriptNamespace.Trim();

        public bool BindButtonClick => _bindButtonClick;

        public IReadOnlyList<Rule> Rules => _rules;

        /// <summary>
        /// 이름에 맞는 규칙을 찾습니다. 여러 개가 맞으면 가장 긴 접두사를 고릅니다. 없으면 null입니다.
        /// </summary>
        public Rule FindRule(string objectName)
        {
            Rule best = null;
            foreach (var rule in _rules)
            {
                if (string.IsNullOrEmpty(rule.Prefix) || !objectName.StartsWith(rule.Prefix, StringComparison.Ordinal))
                    continue;

                if (best == null || rule.Prefix.Length > best.Prefix.Length)
                    best = rule;
            }

            return best;
        }

        /// <summary>
        /// 프로젝트의 설정 파일을 찾습니다. 없으면 기본 규칙으로 새로 만듭니다.
        /// </summary>
        public static UIPrefixSettings GetOrCreate()
        {
            foreach (var guid in AssetDatabase.FindAssets($"t:{nameof(UIPrefixSettings)}"))
            {
                var found = AssetDatabase.LoadAssetAtPath<UIPrefixSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (found != null)
                    return found;
            }

            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
                AssetDatabase.CreateFolder("Assets", "Editor");

            var settings = CreateInstance<UIPrefixSettings>();
            AssetDatabase.CreateAsset(settings, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Nyonyo Framework] UI 접두사 설정 파일을 만들었습니다: {DefaultAssetPath}", settings);
            return settings;
        }

        internal void SetRules(params Rule[] rules) => _rules = rules.ToList();

        private void Reset() => _rules = CreateDefaultRules();

        // _rules 필드 초기화식에서 부르므로 static입니다(초기화식에서는 instance 함수를 부를 수 없습니다).
        private static List<Rule> CreateDefaultRules() => new()
        {
            new Rule("IMG_", "UnityEngine.UI.Image"),
            new Rule("RIMG_", "UnityEngine.UI.RawImage"),
            new Rule("TXT_", "TMPro.TextMeshProUGUI"),
            new Rule("BTN_", "UnityEngine.UI.Button"),
            new Rule("TGL_", "UnityEngine.UI.Toggle"),
            new Rule("SLD_", "UnityEngine.UI.Slider"),
            new Rule("SCR_", "UnityEngine.UI.ScrollRect"),
            new Rule("INP_", "TMPro.TMP_InputField"),
            new Rule("DRP_", "TMPro.TMP_Dropdown"),
            new Rule("RT_", "UnityEngine.RectTransform"),
            new Rule("GO_", "UnityEngine.GameObject"),
            new Rule("UI_", "Nyonyo.Framework.UI.UIBase"),
        };
    }
}
