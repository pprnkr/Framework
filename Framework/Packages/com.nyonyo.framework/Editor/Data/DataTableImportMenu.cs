using System;
using System.IO;
using Nyonyo.Framework.Data;
using UnityEditor;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Data
{
    /// <summary>
    /// 데이터 테이블 에셋의 우클릭(⋮) 메뉴에 Import CSV..., Reimport CSV를 추가합니다.
    /// 마지막으로 가져온 CSV 경로는 테이블마다 기억합니다(이 PC에만 저장).
    /// </summary>
    public static class DataTableImportMenu
    {
        [MenuItem("CONTEXT/DataTableBase/Import CSV...")]
        static void Import(MenuCommand command)
        {
            var table = (DataTableBase)command.context;
            var last = EditorPrefs.GetString(PrefsKey(table), "");
            var folder = string.IsNullOrEmpty(last) ? Application.dataPath : Path.GetDirectoryName(last);
            var path = EditorUtility.OpenFilePanel($"{table.name}에 CSV 가져오기", folder, "csv");
            if (!string.IsNullOrEmpty(path))
                ImportFrom(table, path);
        }

        [MenuItem("CONTEXT/DataTableBase/Reimport CSV")]
        static void Reimport(MenuCommand command)
        {
            var table = (DataTableBase)command.context;
            ImportFrom(table, EditorPrefs.GetString(PrefsKey(table), ""));
        }

        [MenuItem("CONTEXT/DataTableBase/Reimport CSV", true)]
        static bool CanReimport(MenuCommand command) =>
            File.Exists(EditorPrefs.GetString(PrefsKey((DataTableBase)command.context), ""));

        /// <summary>
        /// CSV 파일로 테이블의 모든 행을 바꾸고 저장합니다. 형식이 틀리면 에러를 남기고 테이블은 그대로 둡니다.
        /// </summary>
        public static bool ImportFrom(DataTableBase table, string csvPath)
        {
            if (!File.Exists(csvPath))
            {
                Debug.LogError($"[Nyonyo Framework] CSV 파일 {csvPath}을(를) 찾을 수 없습니다.", table);
                return false;
            }

            string csv;
            try
            {
                // 엑셀이 파일을 열고 있어도 읽을 수 있도록 공유 모드로 엽니다.
                using var stream = new FileStream(csvPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                csv = reader.ReadToEnd();
            }
            catch (IOException e)
            {
                Debug.LogError($"[Nyonyo Framework] CSV 파일을 읽지 못했습니다. {e.Message}", table);
                return false;
            }

            Undo.RecordObject(table, "Import CSV");
            try
            {
                table.ImportCsv(csv);
            }
            catch (FormatException e)
            {
                Debug.LogError($"[Nyonyo Framework] {table.name}: {e.Message}", table);
                return false;
            }

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);
            EditorPrefs.SetString(PrefsKey(table), csvPath);
            Debug.Log($"[Nyonyo Framework] {table.name}에 {Path.GetFileName(csvPath)}의 {table.Count}행을 가져왔습니다.", table);
            return true;
        }

        static string PrefsKey(DataTableBase table) =>
            "Nyonyo.Framework.DataTable.Csv." + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(table));
    }
}
