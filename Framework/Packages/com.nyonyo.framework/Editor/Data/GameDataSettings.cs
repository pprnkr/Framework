using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nyonyo.Framework.Assets;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 구글 스프레드시트에서 게임 데이터를 가져오는 설정입니다. 인스펙터의 <b>가져오기</b> 버튼으로 코드와 데이터 파일을 만듭니다.
    /// </summary>
    /// <remarks>
    /// 이 에셋은 에디터에서만 씁니다. 암호화 키가 들어 있으므로 프로젝트에 함께 커밋해야 다른 사람이 가져와도 같은 키를 씁니다.
    /// </remarks>
    public sealed class GameDataSettings : ScriptableObject
    {
        [Header("스프레드시트")]
        [Tooltip("가져올 구글 스프레드시트 목록입니다.\n" +
                 "1. 오른쪽 아래 + 버튼으로 항목을 추가합니다.\n" +
                 "2. Spreadsheet에 브라우저 주소창의 주소를 그대로 붙여 넣습니다.\n" +
                 "3. Sheets는 비워 두면 이름이 _로 시작하지 않는 모든 시트(탭)를 가져옵니다.\n" +
                 "스프레드시트가 여러 개면 항목을 더 추가하세요. 시트 이름은 서로 겹치면 안 됩니다.")]
        [SerializeField] private List<SheetSource> _spreadsheets = new();

        [Header("코드 생성")]
        [Tooltip("자동 생성 코드(*.g.cs)를 둘 폴더입니다. 없으면 만듭니다.\n" +
                 "예: Assets/GameData/Generated\n" +
                 "이 폴더의 생성 파일은 가져올 때마다 덮어쓰고, 없어진 시트의 파일은 지웁니다(직접 만든 파일은 건드리지 않음).\n" +
                 "코드를 덧붙이려면 다른 폴더(예: Assets/GameData)에 같은 이름의 partial 클래스를 만드세요.")]
        [SerializeField] private string _codeFolder = "Assets/GameData/Generated";

        [Tooltip("생성 코드의 네임스페이스입니다. 게임 코드에서 using으로 씁니다.\n" +
                 "예: Game.Data → using Game.Data;")]
        [SerializeField] private string _namespace = "Game.Data";

        [Tooltip("모든 테이블을 담는 서비스 클래스 이름입니다. 보통 바꿀 필요가 없습니다.\n" +
                 "예: GameData →\n" +
                 "  Installer: services.Register(new GameData(assets));\n" +
                 "  사용: GameData.Instance.Item.Get(1001)")]
        [SerializeField] private string _className = "GameData";

        [Header("데이터 파일")]
        [Tooltip("암호화된 데이터 파일(*.bytes)을 둘 폴더입니다. 없으면 만듭니다.\n" +
                 "예: Assets/GameData/Bytes\n" +
                 "가져올 때 이 폴더의 .bytes 중 시트에 없는 파일은 지우므로, 이 폴더는 게임 데이터 전용으로 쓰세요.\n" +
                 "Load Type이 Resources면 Resources 폴더 안이어야 합니다(예: Assets/Resources/GameData).")]
        [SerializeField] private string _dataFolder = "Assets/GameData/Bytes";

        [Tooltip("게임 실행 중 데이터 파일을 불러오는 방식입니다.\n" +
                 "• Addressables(추천): 가져올 때 Addressables에 자동 등록합니다. 데이터 폴더는 아무 곳이나 됩니다.\n" +
                 "• Resources: 데이터 폴더가 Resources 폴더 안에 있어야 합니다.")]
        [SerializeField] private LoadType _loadType = LoadType.Addressables;

        [Tooltip("Load Type이 Addressables일 때 등록할 address 형식입니다. {0}에 시트 이름이 들어갑니다.\n" +
                 "예: GameData/{0} → Item 시트는 GameData/Item\n" +
                 "Resources일 때는 쓰지 않습니다.")]
        [SerializeField] private string _addressFormat = "GameData/{0}";

        [SerializeField, HideInInspector] private string _encryptionKey = "";

        public IReadOnlyList<SheetSource> Spreadsheets => _spreadsheets;

        public string CodeFolder => _codeFolder;

        public string Namespace => _namespace;

        public string ClassName => _className;

        public string DataFolder => _dataFolder;

        public LoadType LoadType => _loadType;

        public string AddressFormat => _addressFormat;

        public bool HasEncryptionKey => !string.IsNullOrEmpty(_encryptionKey);

        /// <summary>
        /// 이 컴퓨터의 구글 서비스 계정 키(JSON) 파일 경로입니다. 키가 git에 올라가지 않도록 에셋이 아니라 EditorPrefs에 저장합니다.
        /// 비어 있으면 환경 변수 GOOGLE_APPLICATION_CREDENTIALS를 씁니다(빌드 서버용).
        /// </summary>
        public string ServiceAccountKeyPath
        {
            get
            {
                var path = EditorPrefs.GetString(KeyPathPrefsKey, "");
                return path.Length > 0 ? path : Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") ?? "";
            }
            set => EditorPrefs.SetString(KeyPathPrefsKey, value ?? "");
        }

        // EditorPrefs는 모든 프로젝트가 같이 쓰므로 이 에셋의 GUID를 붙여 구분합니다.
        private string KeyPathPrefsKey => "Nyonyo.GameData.ServiceAccountKey." + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(this));

        /// <summary>암호화 키입니다. 없으면 새로 만들어 이 에셋에 저장합니다.</summary>
        internal byte[] GetOrCreateEncryptionKey()
        {
            if (!HasEncryptionKey)
                ResetEncryptionKey();
            return Convert.FromBase64String(_encryptionKey);
        }

        /// <summary>새 암호화 키를 만듭니다. 이전 키로 만든 데이터 파일은 다시 가져와야 읽을 수 있습니다.</summary>
        internal void ResetEncryptionKey()
        {
            var key = new byte[Nyonyo.Framework.Data.GameDataCrypto.KeySize];
            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(key);

            _encryptionKey = Convert.ToBase64String(key);
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssetIfDirty(this);
        }
    }
}
