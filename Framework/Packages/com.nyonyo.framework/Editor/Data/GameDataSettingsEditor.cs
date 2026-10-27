using System;
using System.IO;
using Nyonyo.Framework.Assets;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// <see cref="GameDataSettings"/> 인스펙터입니다. 처음 설정 안내, 서비스 계정 키 선택, 가져오기 버튼, 시트 작성 규칙을 보여 줍니다.
    /// </summary>
    [CustomEditor(typeof(GameDataSettings))]
    internal sealed class GameDataSettingsEditor : UnityEditor.Editor
    {
        private const string GuideFoldoutKey = "Nyonyo.GameDataSettings.ShowGuide";
        private const string RulesFoldoutKey = "Nyonyo.GameDataSettings.ShowRules";

        private const string Guide =
            "한 번만 하면 되는 준비 (Google Cloud Console, console.cloud.google.com)\n" +
            "1. 프로젝트를 고르거나 만들고, API 및 서비스 > 라이브러리에서 'Google Drive API'를 사용 설정\n" +
            "2. IAM 및 관리자 > 서비스 계정 > 서비스 계정 만들기 (역할은 주지 않아도 됨)\n" +
            "3. 만든 서비스 계정 > 키 > 키 추가 > 새 키 만들기 > JSON → 파일이 내려받아짐\n" +
            "4. 받은 JSON 파일을 git에 올라가지 않는 곳에 두고, 아래 [키 파일 선택]으로 고르기\n\n" +
            "스프레드시트마다\n" +
            "5. 아래 [이메일 복사] → 구글 시트 [공유]에 붙여 넣고 '뷰어'로 추가 (일반 액세스는 '제한됨' 그대로)\n" +
            "6. Spreadsheets의 + 버튼 → Spreadsheet에 브라우저 주소 붙여 넣기 (Sheets는 비워 두면 모든 시트)\n" +
            "7. 맨 아래 [가져오기] → 코드(*.g.cs)와 데이터(*.bytes)가 만들어짐\n\n" +
            "게임 코드\n" +
            "8. Installer에 등록: services.Register(new GameData(assets));\n" +
            "9. 사용: GameData.Instance.Item.Get(1001)\n\n" +
            "다른 항목은 기본값 그대로 써도 됩니다. 항목에 마우스를 올리면 설명이 나옵니다.";

        private const string Rules =
            "1행 주석    | 아이템 ID | 공격력 | 공격력 | 이름\n" +
            "2행 자료형  | int       | int[]  | int[]  | string\n" +
            "3행 변수명  | Id        | Attack | Attack | Name\n" +
            "4행~ 데이터 | 1001      | 10     | 20     | 검      → Attack[0]=10, Attack[1]=20\n\n" +
            "• 첫 번째 열은 유니크 키(int, long, string). 겹치면 에러\n" +
            "• 자료형: int, long, float, double, bool, string, enum 이름, 배열(int[])\n" +
            "• 배열은 같은 변수명의 열들이 왼쪽부터 원소. 빈 칸은 건너뜀\n" +
            "• 변수명이 _로 시작하는 열, 키가 _로 시작하는 행, 이름이 _로 시작하는 시트는 건너뜀";

        private readonly GUIContent _keyLabel = new("서비스 계정 키",
            "구글 시트를 읽을 서비스 계정의 JSON 키 파일입니다.\n" +
            "경로는 이 컴퓨터에만 저장되고 에셋에는 들어가지 않습니다. 팀원마다 각자 고르세요.\n" +
            "키 파일은 비밀번호와 같으니 git, 메신저 등에 올리지 마세요.");

        private readonly GUIContent _chooseKeyButton = new("키 파일 선택",
            "Google Cloud Console > 서비스 계정 > 키 > 키 추가 > JSON으로 받은 파일을 고릅니다.");

        private readonly GUIContent _clearKeyButton = new("지우기", "이 컴퓨터에 저장한 키 파일 경로를 지웁니다. 파일은 지우지 않습니다.");

        private readonly GUIContent _emailLabel = new("서비스 계정 이메일",
            "이 이메일을 스프레드시트 [공유]에 '뷰어'로 추가해야 가져올 수 있습니다.\n" +
            "시트의 일반 액세스는 '제한됨'으로 두어도 됩니다.");

        private readonly GUIContent _copyEmailButton = new("이메일 복사",
            "서비스 계정 이메일을 클립보드에 복사합니다. 구글 시트 [공유] 창에 붙여 넣고 '뷰어'로 추가하세요.");

        private readonly GUIContent _importButton = new("가져오기",
            "구글 시트를 내려받아 코드(*.g.cs)와 암호화된 데이터(*.bytes)를 만듭니다.\n" +
            "시트에 문제가 있으면 셀 위치(예: Item!C5)를 콘솔에 모두 보여 주고, 파일은 하나도 바꾸지 않습니다.\n" +
            "열 구성이 바뀌면 끝난 뒤 컴파일이 이어서 일어납니다.");

        private readonly GUIContent _openButton = new("스프레드시트 열기", "등록한 스프레드시트를 브라우저로 엽니다.");

        private readonly GUIContent _resetKeyButton = new("암호화 키 새로 만들기",
            "데이터 파일을 암호화하는 키를 새로 만듭니다. 보통은 쓸 일이 없습니다.\n" +
            "새로 만들면 지금 데이터 파일은 읽을 수 없으므로 이어서 다시 가져와야 합니다.");

        // 인스펙터는 자주 다시 그려지므로, 키 파일은 경로나 수정 시각이 바뀔 때만 다시 읽습니다.
        private string _readKeyPath;
        private DateTime _readKeyTime;
        private string _clientEmail;
        private string _keyError;

        public override void OnInspectorGUI()
        {
            var settings = (GameDataSettings)target;

            // 처음 설정할 때는 안내를 펼쳐 두고, 스프레드시트를 넣은 뒤에는 접어 둡니다(사용자가 바꾸면 그 상태를 기억).
            var showGuide = SessionState.GetBool(GuideFoldoutKey, settings.Spreadsheets.Count == 0);
            showGuide = EditorGUILayout.BeginFoldoutHeaderGroup(showGuide, "처음 설정하는 방법");
            if (showGuide)
                EditorGUILayout.HelpBox(Guide, MessageType.Info);
            EditorGUILayout.EndFoldoutHeaderGroup();
            SessionState.SetBool(GuideFoldoutKey, showGuide);

            var showRules = SessionState.GetBool(RulesFoldoutKey, false);
            showRules = EditorGUILayout.BeginFoldoutHeaderGroup(showRules, "시트 작성 규칙");
            if (showRules)
                EditorGUILayout.HelpBox(Rules, MessageType.None);
            EditorGUILayout.EndFoldoutHeaderGroup();
            SessionState.SetBool(RulesFoldoutKey, showRules);

            EditorGUILayout.Space();
            DrawServiceAccount(settings);

            EditorGUILayout.Space();
            DrawDefaultInspector();
            DrawProblems(settings);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(new GUIContent("암호화 키", "데이터 파일 암호화 키입니다. 이 에셋에 저장되므로 에셋을 git에 함께 올려야 다른 사람도 같은 키로 가져옵니다."),
                new GUIContent(settings.HasEncryptionKey ? "있음 (이 에셋에 저장됨)" : "없음 (처음 가져올 때 자동으로 만듦)"));

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(settings.Spreadsheets.Count == 0 || _clientEmail == null))
            {
                if (GUILayout.Button(_importButton, GUILayout.Height(30)))
                    EditorApplication.delayCall += () => new GameDataImporter(settings).Import();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_openButton))
                    OpenSpreadsheets(settings);

                if (GUILayout.Button(_resetKeyButton) &&
                    EditorUtility.DisplayDialog("암호화 키 새로 만들기",
                        "새 키를 만들면 지금 데이터 파일은 읽을 수 없습니다. 이어서 다시 가져오세요.", "새로 만들기", "취소"))
                {
                    settings.ResetEncryptionKey();
                }
            }
        }

        private void DrawServiceAccount(GameDataSettings settings)
        {
            EditorGUILayout.LabelField("구글 서비스 계정 (이 컴퓨터에만 저장)", EditorStyles.boldLabel);

            var path = settings.ServiceAccountKeyPath;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_keyLabel, new GUIContent(string.IsNullOrEmpty(path) ? "(없음)" : Path.GetFileName(path), path));
                if (GUILayout.Button(_chooseKeyButton, GUILayout.Width(90)))
                {
                    var folder = string.IsNullOrEmpty(path) ? "" : Path.GetDirectoryName(path);
                    var chosen = EditorUtility.OpenFilePanel("서비스 계정 JSON 키 선택", folder, "json");
                    if (!string.IsNullOrEmpty(chosen))
                        settings.ServiceAccountKeyPath = chosen;
                }
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(path)))
                {
                    if (GUILayout.Button(_clearKeyButton, GUILayout.Width(50)))
                        settings.ServiceAccountKeyPath = "";
                }
            }

            path = settings.ServiceAccountKeyPath;
            ReadKey(path);
            if (string.IsNullOrEmpty(path))
            {
                EditorGUILayout.HelpBox("서비스 계정 JSON 키를 고르세요. 처음이면 위 '처음 설정하는 방법'을 펼쳐 1~4단계를 따라 하세요.", MessageType.Warning);
                return;
            }
            if (_keyError != null)
            {
                EditorGUILayout.HelpBox(_keyError, MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(_emailLabel, new GUIContent(_clientEmail, _clientEmail));
                if (GUILayout.Button(_copyEmailButton, GUILayout.Width(90)))
                {
                    EditorGUIUtility.systemCopyBuffer = _clientEmail;
                    ShowNotification("이메일을 복사했습니다. 구글 시트 [공유]에 붙여 넣고 '뷰어'로 추가하세요.");
                }
            }

            if (IsInsideRepository(path))
                EditorGUILayout.HelpBox("키 파일이 프로젝트나 git 저장소 폴더 안에 있습니다. 실수로 커밋되지 않게 저장소 밖(예: 문서 폴더)으로 옮기세요.", MessageType.Warning);
        }

        private void ReadKey(string path)
        {
            var time = !string.IsNullOrEmpty(path) && File.Exists(path) ? File.GetLastWriteTimeUtc(path) : default;
            if (path == _readKeyPath && time == _readKeyTime)
                return;

            _readKeyPath = path;
            _readKeyTime = time;
            _clientEmail = null;
            _keyError = null;
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                _clientEmail = new ServiceAccountKey(path).ClientEmail;
            }
            catch (Exception e) when (e is IOException || e is InvalidDataException || e is UnauthorizedAccessException)
            {
                _keyError = e.Message;
            }
        }

        // 프로젝트 폴더나, 그 위의 git 저장소(.git이 있는 폴더) 안에 있는지 확인합니다.
        private bool IsInsideRepository(string path)
        {
            var file = Path.GetFullPath(path);
            var folder = new DirectoryInfo(Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
            var root = folder.FullName;
            for (var current = folder; current != null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, ".git")))
                {
                    root = current.FullName;
                    break;
                }
            }
            return file.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        // 가져오기를 누르기 전에 알 수 있는 입력 실수를 바로 보여 줍니다.
        private void DrawProblems(GameDataSettings settings)
        {
            if (settings.Spreadsheets.Count == 0)
                EditorGUILayout.HelpBox("Spreadsheets가 비어 있습니다. + 버튼을 눌러 스프레드시트 주소를 넣으세요.", MessageType.Warning);

            for (var i = 0; i < settings.Spreadsheets.Count; i++)
            {
                if (settings.Spreadsheets[i].GetSpreadsheetId() == null)
                    EditorGUILayout.HelpBox($"Spreadsheets 항목 {i}의 주소가 올바르지 않습니다. 구글 시트를 열고 브라우저 주소창의 주소를 그대로 붙여 넣으세요.", MessageType.Warning);
            }

            if (settings.LoadType == LoadType.Resources && !(settings.DataFolder ?? "").Replace('\\', '/').Contains("/Resources"))
                EditorGUILayout.HelpBox("Load Type이 Resources면 Data Folder가 Resources 폴더 안에 있어야 합니다(예: Assets/Resources/GameData).", MessageType.Warning);
            else if (settings.LoadType == LoadType.Addressables && !(settings.AddressFormat ?? "").Contains("{0}"))
                EditorGUILayout.HelpBox("Address Format에 시트 이름이 들어갈 {0}이 필요합니다(예: GameData/{0}).", MessageType.Warning);
        }

        private void ShowNotification(string message)
        {
            var window = EditorWindow.focusedWindow;
            if (window != null)
                window.ShowNotification(new GUIContent(message));
            Debug.Log("[Nyonyo Framework] " + message);
        }

        [MenuItem("Tools/Nyonyo Framework/Game Data/Import All")]
        private static void ImportAll()
        {
            var guids = AssetDatabase.FindAssets("t:" + nameof(GameDataSettings));
            if (guids.Length == 0)
            {
                EditorUtility.DisplayDialog("게임 데이터 가져오기", "GameDataSettings 에셋이 없습니다. Tools > Nyonyo Framework > Settings > Game Data Settings로 만드세요.", "확인");
                return;
            }

            foreach (var guid in guids)
            {
                var settings = AssetDatabase.LoadAssetAtPath<GameDataSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (settings != null && !new GameDataImporter(settings).Import())
                    return;
            }
        }

        private void OpenSpreadsheets(GameDataSettings settings)
        {
            foreach (var source in settings.Spreadsheets)
            {
                var id = source.GetSpreadsheetId();
                if (id != null)
                    Application.OpenURL($"https://docs.google.com/spreadsheets/d/{id}/edit");
            }
        }
    }
}
