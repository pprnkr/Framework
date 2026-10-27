using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Nyonyo.Framework.Assets;
using Nyonyo.Framework.Data;
using Nyonyo.Framework.Editor.Assets;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// <see cref="GameDataSettings"/>의 구글 스프레드시트를 내려받아, 코드(*.g.cs)와 암호화된 데이터 파일(*.bytes)을 만듭니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 순서: 서비스 계정으로 액세스 토큰 받기 → Drive API로 xlsx 내려받기 → 시트 해석과 규칙 검사 →
    /// 에러가 하나라도 있으면 아무 파일도 바꾸지 않고 멈춤 → 코드와 데이터 파일 쓰기(내용이 같으면 그대로 둠) →
    /// 없어진 시트의 생성 파일 정리 → Addressables 등록.
    /// </para>
    /// <para>
    /// 시트 공유는 "제한됨"으로 두고 서비스 계정 이메일만 뷰어로 추가합니다. 서비스 계정의 Google Cloud 프로젝트에서 Google Drive API를 켜야 합니다.
    /// Drive API의 내보내기는 스프레드시트 하나당 10MB까지입니다.
    /// </para>
    /// <para>데이터 파일은 생성 코드와 상관없이 시트 내용만으로 만들므로, 열 구성이 바뀌어도 한 번에 끝납니다(컴파일은 그 뒤에 일어남).</para>
    /// </remarks>
    public sealed class GameDataImporter
    {
        private const string ExportUrlFormat =
            "https://www.googleapis.com/drive/v3/files/{0}/export?mimeType=application%2Fvnd.openxmlformats-officedocument.spreadsheetml.sheet";
        private const string ReadOnlyScope = "https://www.googleapis.com/auth/drive.readonly";
        private const double TimeoutSeconds = 60;
        private const string ProgressTitle = "게임 데이터 가져오기";

        private static readonly UTF8Encoding s_utf8 = new(false);

        // 시트 이름이 서비스 클래스의 속성 이름이 되므로, 기반 클래스 멤버와 겹치면 안 됩니다.
        private static readonly HashSet<string> s_reservedSheetNames = new()
        {
            "Tables", "IsLoaded", "Instance", "LoadAll", "LoadAllAsync", "InitializeAsync", "OnLoaded", "LoadType", "PathFormat", "EncryptionKey",
        };

        private readonly GameDataSettings _settings;

        public GameDataImporter(GameDataSettings settings)
        {
            _settings = settings ? settings : throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>가져오기를 실행합니다. 에러는 콘솔에 모두 남기고 창으로도 알립니다.</summary>
        /// <returns>성공하면 true입니다.</returns>
        public bool Import()
        {
            var errors = new List<string>();
            try
            {
                var pathFormat = ValidateSettings(errors);
                if (errors.Count > 0)
                    return Report(errors);

                var tables = DownloadAndParse(errors);
                if (errors.Count > 0)
                    return Report(errors);

                var key = _settings.GetOrCreateEncryptionKey();
                var generator = new GameDataCodeGenerator(_settings.Namespace.Trim(), _settings.ClassName.Trim(), _settings.LoadType, pathFormat, key);
                var changed = WriteCode(tables, generator);
                changed |= WriteData(tables, key);
                AssetDatabase.Refresh();
                if (_settings.LoadType == LoadType.Addressables)
                    RegisterAddresses(tables);

                var summary = new StringBuilder();
                foreach (var table in tables)
                    summary.Append($" {table.Name}({table.Rows.Count}행)");
                Debug.Log($"[Nyonyo Framework] 게임 데이터 {tables.Count}개 테이블을 가져왔습니다:{summary}{(changed ? "" : " (바뀐 내용 없음)")}", _settings);
                return true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[Nyonyo Framework] 게임 데이터 가져오기를 취소했습니다. 파일은 바뀌지 않았습니다.");
                return false;
            }
            catch (Exception e)
            {
                errors.Add(e.Message);
                return Report(errors);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        #region 설정 검사

        // 데이터 파일 경로 형식을 돌려줍니다(Resources면 Resources 폴더 기준 경로, Addressables면 address 형식).
        private string ValidateSettings(List<string> errors)
        {
            var parser = new SheetTableParser(FindEnum);
            if (string.IsNullOrWhiteSpace(_settings.ServiceAccountKeyPath))
                errors.Add("서비스 계정 키 파일이 없습니다. Game Data Settings 인스펙터의 [키 파일 선택]으로 JSON 키를 고르세요.");
            if (_settings.Spreadsheets.Count == 0)
                errors.Add("가져올 스프레드시트가 없습니다. Spreadsheets에 스프레드시트 주소를 넣으세요.");
            for (var i = 0; i < _settings.Spreadsheets.Count; i++)
            {
                if (_settings.Spreadsheets[i].GetSpreadsheetId() == null)
                    errors.Add($"Spreadsheets[{i}]: 스프레드시트 주소나 ID가 올바르지 않습니다. 브라우저 주소창의 주소를 그대로 붙여 넣으세요.");
            }

            foreach (var part in (_settings.Namespace ?? "").Trim().Split('.'))
            {
                if (!parser.IsValidIdentifier(part))
                {
                    errors.Add($"Namespace '{_settings.Namespace}'가 올바르지 않습니다(예: Game.Data).");
                    break;
                }
            }
            if (!parser.IsValidIdentifier((_settings.ClassName ?? "").Trim()))
                errors.Add($"Class Name '{_settings.ClassName}'이 올바르지 않습니다(예: GameData).");

            var codeFolder = NormalizeFolder(_settings.CodeFolder);
            var dataFolder = NormalizeFolder(_settings.DataFolder);
            if (!codeFolder.StartsWith("Assets/"))
                errors.Add($"Code Folder '{_settings.CodeFolder}'는 Assets/ 아래여야 합니다.");
            if (!dataFolder.StartsWith("Assets/"))
                errors.Add($"Data Folder '{_settings.DataFolder}'는 Assets/ 아래여야 합니다.");

            if (_settings.LoadType == LoadType.Addressables)
            {
                if (_settings.AddressFormat == null || !_settings.AddressFormat.Contains("{0}"))
                    errors.Add("Address Format에 시트 이름이 들어갈 {0}이 필요합니다(예: GameData/{0}).");
                return _settings.AddressFormat;
            }

            var resources = dataFolder.LastIndexOf("/Resources", StringComparison.Ordinal);
            if (resources < 0 || (dataFolder.Length > resources + 10 && dataFolder[resources + 10] != '/'))
            {
                errors.Add($"Load Type이 Resources면 Data Folder가 Resources 폴더 안에 있어야 합니다(예: Assets/Resources/GameData). 지금은 '{dataFolder}'입니다.");
                return "";
            }
            var relative = dataFolder.Substring(resources + "/Resources".Length).TrimStart('/');
            return relative.Length == 0 ? "{0}" : relative + "/{0}";
        }

        #endregion

        #region 내려받기와 해석

        private List<SheetTable> DownloadAndParse(List<string> errors)
        {
            var parser = new SheetTableParser(FindEnum);
            var reader = new XlsxReader();
            var tables = new List<SheetTable>();
            var names = new HashSet<string>();

            var account = new ServiceAccountKey(_settings.ServiceAccountKeyPath);
            var accessToken = RequestAccessToken(account);

            for (var i = 0; i < _settings.Spreadsheets.Count; i++)
            {
                var source = _settings.Spreadsheets[i];
                var sheets = reader.Read(Download(source.GetSpreadsheetId(), accessToken, account.ClientEmail, i));
                foreach (var sheet in SelectSheets(source, sheets, errors))
                {
                    var table = parser.Parse(sheet.Name, sheet.Rows, errors);
                    if (table == null)
                        continue;

                    if (!names.Add(table.Name))
                        errors.Add($"{table.Name}: 같은 이름의 시트가 두 번 있습니다. 시트 이름은 클래스 이름이 되므로 스프레드시트가 달라도 겹치면 안 됩니다.");
                    else if (s_reservedSheetNames.Contains(table.Name) || table.Name == _settings.ClassName.Trim())
                        errors.Add($"{table.Name}: 이 시트 이름은 {_settings.ClassName}의 멤버와 겹쳐서 쓸 수 없습니다. 시트 이름을 바꾸세요.");
                    else
                        tables.Add(table);
                }
            }

            return tables;
        }

        private List<XlsxReader.Sheet> SelectSheets(SheetSource source, List<XlsxReader.Sheet> sheets, List<string> errors)
        {
            var selected = new List<XlsxReader.Sheet>();
            if (source.Sheets.Count == 0)
            {
                foreach (var sheet in sheets)
                {
                    if (!sheet.Name.StartsWith("_"))
                        selected.Add(sheet);
                }
                return selected;
            }

            foreach (var name in source.Sheets)
            {
                var wanted = (name ?? "").Trim();
                if (wanted.Length == 0)
                    continue;

                var sheet = sheets.Find(s => s.Name == wanted);
                if (sheet != null)
                    selected.Add(sheet);
                else
                    errors.Add($"시트 '{wanted}'가 스프레드시트에 없습니다. 있는 시트: {string.Join(", ", sheets.ConvertAll(s => s.Name))}");
            }
            return selected;
        }

        // 서비스 계정 키로 서명한 JWT를 보내 1시간짜리 액세스 토큰을 받습니다.
        private string RequestAccessToken(ServiceAccountKey account)
        {
            var assertion = new ServiceAccountAssertion().Create(account.ClientEmail, ReadOnlyScope, account.TokenUri, DateTimeOffset.UtcNow, account.PrivateKey);
            var form = new Dictionary<string, string>
            {
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:jwt-bearer",
                ["assertion"] = assertion,
            };

            using var request = UnityWebRequest.Post(account.TokenUri, form);
            Send(request, "구글 서비스 계정으로 인증하는 중", 0f);

            var response = ParseJson<TokenResponse>(request.downloadHandler.text);
            if (request.result == UnityWebRequest.Result.Success && !string.IsNullOrEmpty(response.access_token))
                return response.access_token;

            var reason = string.IsNullOrEmpty(response.error) ? request.error : $"{response.error} {response.error_description}";
            var hint = response.error == "invalid_grant"
                ? "키가 삭제되었거나 이 컴퓨터의 시계가 틀렸을 수 있습니다. 시계를 맞추거나 서비스 계정에서 새 키를 받으세요."
                : "키 파일이 맞는지, 인터넷에 연결되어 있는지 확인하세요.";
            throw new InvalidOperationException($"서비스 계정({account.ClientEmail}) 인증에 실패했습니다: {reason}. {hint}");
        }

        private byte[] Download(string spreadsheetId, string accessToken, string clientEmail, int index)
        {
            using var request = UnityWebRequest.Get(string.Format(ExportUrlFormat, spreadsheetId));
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            Send(request, $"스프레드시트 내려받는 중 ({index + 1}/{_settings.Spreadsheets.Count})", (float)index / _settings.Spreadsheets.Count);

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException(DescribeDownloadError(request, spreadsheetId, clientEmail));

            var data = request.downloadHandler.data;
            if (data == null || data.Length < 2 || data[0] != 'P' || data[1] != 'K')
                throw new InvalidOperationException($"스프레드시트({spreadsheetId})에서 xlsx가 아닌 응답이 왔습니다. 구글 시트 주소가 맞는지 확인하세요.");
            return data;
        }

        // Drive API 에러 응답을 원인과 해결 방법으로 바꿉니다.
        private string DescribeDownloadError(UnityWebRequest request, string spreadsheetId, string clientEmail)
        {
            var body = request.downloadHandler?.text ?? "";
            var prefix = $"스프레드시트({spreadsheetId})를 내려받지 못했습니다(HTTP {request.responseCode}). ";

            if (body.Contains("accessNotConfigured") || body.Contains("SERVICE_DISABLED") || body.Contains("has not been used"))
                return prefix + "서비스 계정의 Google Cloud 프로젝트에서 Google Drive API가 꺼져 있습니다. " +
                       "Google Cloud Console > API 및 서비스 > 라이브러리에서 'Google Drive API'를 사용 설정하세요(켠 뒤 몇 분 걸릴 수 있음).";
            if (body.Contains("exportSizeLimitExceeded"))
                return prefix + "스프레드시트가 Drive 내보내기 한도(10MB)를 넘었습니다. 시트를 여러 스프레드시트로 나누세요.";
            if (body.Contains("fileNotExportable"))
                return prefix + "구글 시트 형식이 아닙니다(엑셀 파일을 그대로 올린 경우). 구글 시트에서 파일 > Google Sheets로 저장한 뒤 그 주소를 쓰세요.";
            if (request.responseCode == 404 || request.responseCode == 403)
                return prefix + $"주소가 맞는지, 그리고 스프레드시트 [공유]에 서비스 계정 {clientEmail}을 뷰어로 추가했는지 확인하세요.";
            return prefix + $"{request.error} {body}";
        }

        // 진행 표시줄을 보여 주며 요청이 끝날 때까지 기다립니다. 취소 버튼과 시간 초과를 처리합니다.
        private void Send(UnityWebRequest request, string message, float progress)
        {
            var operation = request.SendWebRequest();
            var started = EditorApplication.timeSinceStartup;
            while (!operation.isDone)
            {
                if (EditorUtility.DisplayCancelableProgressBar(ProgressTitle, message, progress))
                {
                    request.Abort();
                    throw new OperationCanceledException();
                }
                if (EditorApplication.timeSinceStartup - started > TimeoutSeconds)
                {
                    request.Abort();
                    throw new TimeoutException($"구글 서버가 {TimeoutSeconds}초 안에 응답하지 않았습니다. 네트워크를 확인하세요.");
                }
                Thread.Sleep(20);
            }
        }

        private T ParseJson<T>(string json) where T : new()
        {
            try
            {
                return string.IsNullOrEmpty(json) ? new T() : JsonUtility.FromJson<T>(json) ?? new T();
            }
            catch (ArgumentException)
            {
                return new T();
            }
        }

        // JsonUtility가 응답 JSON 키 이름 그대로 채우도록 필드 이름을 응답과 똑같이 둡니다.
        [Serializable]
        private sealed class TokenResponse
        {
            public string access_token = "";
            public string error = "";
            public string error_description = "";
        }

        // 시트 자료형에 적힌 enum 이름을 프로젝트의 enum에서 찾습니다. 전체 이름이 맞으면 그것을, 아니면 게임 어셈블리에서 짧은 이름으로 찾습니다.
        private Type FindEnum(string name, out string error)
        {
            error = null;
            var exact = new List<Type>();
            var byName = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Enum>())
            {
                if (type.FullName == null || type.ContainsGenericParameters)
                    continue;
                if (type.FullName == name || type.FullName.Replace('+', '.') == name)
                    exact.Add(type);
                else if (type.Name == name && IsGameAssembly(type.Assembly.GetName().Name))
                    byName.Add(type);
            }

            var candidates = exact.Count > 0 ? exact : byName;
            if (candidates.Count == 1)
                return candidates[0];

            error = candidates.Count == 0
                ? $"enum '{name}'도 찾지 못했습니다"
                : $"enum '{name}'이 여러 개입니다: {string.Join(", ", candidates.ConvertAll(t => t.FullName))}. 네임스페이스까지 적으세요";
            return null;
        }

        private bool IsGameAssembly(string assembly) =>
            !assembly.StartsWith("Unity") && !assembly.StartsWith("System") && !assembly.StartsWith("Mono.") &&
            assembly != "mscorlib" && assembly != "netstandard" && !assembly.StartsWith("nunit");

        #endregion

        #region 쓰기

        private bool WriteCode(List<SheetTable> tables, GameDataCodeGenerator generator)
        {
            var folder = NormalizeFolder(_settings.CodeFolder);
            Directory.CreateDirectory(folder);

            var written = new HashSet<string>();
            var changed = false;
            foreach (var table in tables)
                changed |= WriteFile(folder, table.RowClassName + ".g.cs", generator.GenerateTable(table), written);
            changed |= WriteFile(folder, _settings.ClassName.Trim() + ".g.cs", generator.GenerateRoot(tables), written);

            // 없어진 시트의 생성 파일을 지웁니다. 표시가 있는 파일만 지우므로 직접 만든 파일은 건드리지 않습니다.
            foreach (var path in Directory.GetFiles(folder, "*.g.cs"))
            {
                var file = Path.GetFileName(path);
                if (written.Contains(file) || !File.ReadAllText(path).StartsWith(GameDataCodeGenerator.Marker))
                    continue;
                AssetDatabase.DeleteAsset($"{folder}/{file}");
                changed = true;
            }
            return changed;
        }

        private bool WriteData(List<SheetTable> tables, byte[] key)
        {
            var folder = NormalizeFolder(_settings.DataFolder);
            Directory.CreateDirectory(folder);

            var builder = new GameDataBinaryBuilder();
            var crypto = new GameDataCrypto(key);
            var written = new HashSet<string>();
            var changed = false;
            foreach (var table in tables)
            {
                var file = table.Name + ".bytes";
                var bytes = crypto.Encrypt(builder.Build(table));
                var path = $"{folder}/{file}";
                written.Add(file);
                if (File.Exists(path) && AreEqual(File.ReadAllBytes(path), bytes))
                    continue;
                File.WriteAllBytes(path, bytes);
                changed = true;
            }

            foreach (var path in Directory.GetFiles(folder, "*.bytes"))
            {
                var file = Path.GetFileName(path);
                if (written.Contains(file))
                    continue;
                AssetDatabase.DeleteAsset($"{folder}/{file}");
                changed = true;
            }
            return changed;
        }

        private void RegisterAddresses(List<SheetTable> tables)
        {
            var folder = NormalizeFolder(_settings.DataFolder);
            foreach (var table in tables)
                AddressableEditorUtility.SetAddress($"{folder}/{table.Name}.bytes", string.Format(_settings.AddressFormat, table.Name));
        }

        private bool WriteFile(string folder, string file, string content, HashSet<string> written)
        {
            written.Add(file);
            var path = $"{folder}/{file}";
            if (File.Exists(path) && File.ReadAllText(path) == content)
                return false;
            File.WriteAllText(path, content, s_utf8);
            return true;
        }

        private bool AreEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        #endregion

        private string NormalizeFolder(string folder) => (folder ?? "").Trim().Replace('\\', '/').TrimEnd('/');

        private bool Report(List<string> errors)
        {
            var message = string.Join("\n", errors);
            Debug.LogError($"[Nyonyo Framework] 게임 데이터를 가져오지 못했습니다. 파일은 바뀌지 않았습니다. ({errors.Count}건)\n{message}", _settings);
            var shown = errors.Count > 10 ? string.Join("\n", errors.GetRange(0, 10)) + $"\n... 외 {errors.Count - 10}건 (콘솔 참고)" : message;
            EditorUtility.DisplayDialog(ProgressTitle, shown, "확인");
            return false;
        }
    }
}
