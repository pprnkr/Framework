using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 가져올 구글 스프레드시트 하나와 그 안의 시트 목록입니다.
    /// </summary>
    [Serializable]
    public sealed class SheetSource
    {
        private static readonly Regex s_urlIdPattern = new(@"/spreadsheets/d/([A-Za-z0-9_-]+)");
        private static readonly Regex s_idPattern = new(@"^[A-Za-z0-9_-]{20,}$");

        [Tooltip("브라우저 주소창의 스프레드시트 주소를 그대로 붙여 넣으세요(ID만 넣어도 됩니다).\n" +
                 "예: https://docs.google.com/spreadsheets/d/1AbC.../edit#gid=0\n\n" +
                 "먼저 구글 시트 오른쪽 위 [공유]에서 서비스 계정 이메일(위 '서비스 계정 이메일'의 [이메일 복사])을\n" +
                 "'뷰어'로 추가해야 합니다. 일반 액세스는 '제한됨' 그대로 두면 됩니다.")]
        [SerializeField] private string _spreadsheet = "";

        [Tooltip("가져올 시트(화면 아래 탭) 이름입니다. 탭에 적힌 이름과 똑같이 적으세요(대소문자 구분).\n" +
                 "비워 두면 이름이 _로 시작하지 않는 모든 시트를 가져옵니다(추천).\n\n" +
                 "시트 이름은 클래스 이름이 되므로 영문, 숫자, _만 씁니다.\n" +
                 "예: Item → ItemData(행), ItemTable(테이블), GameData.Instance.Item\n" +
                 "가져오지 않을 시트(메모, 계산용)는 이름을 _로 시작하세요(예: _Memo).")]
        [SerializeField] private List<string> _sheets = new();

        public string Spreadsheet => _spreadsheet;

        public IReadOnlyList<string> Sheets => _sheets;

        /// <summary>주소나 ID에서 스프레드시트 ID를 꺼냅니다. 형식이 맞지 않으면 null입니다.</summary>
        public string GetSpreadsheetId()
        {
            var text = (_spreadsheet ?? "").Trim();
            var match = s_urlIdPattern.Match(text);
            if (match.Success)
                return match.Groups[1].Value;
            return s_idPattern.IsMatch(text) ? text : null;
        }
    }
}
