using System.IO;
using System.Text;
using UnityEngine;

namespace Nyonyo.Framework.Save
{
    /// <summary>
    /// 키마다 JSON 파일 하나로 저장합니다. 기본 폴더는 <c>Application.persistentDataPath/Saves</c>입니다.
    /// </summary>
    /// <remarks>
    /// 임시 파일에 먼저 쓴 뒤 바꿔치기해서, 쓰는 도중 꺼져도 이전 데이터가 깨지지 않습니다.
    /// 바꿔치기 도중 꺼져서 본 파일이 없으면 임시 파일을 읽습니다.
    /// </remarks>
    public sealed class FileSaveStorage : ISaveStorage
    {
        private const string Extension = ".json";
        private const string TempExtension = ".json.tmp";

        private static readonly Encoding s_encoding = new UTF8Encoding(false);

        public FileSaveStorage(string folder = null)
        {
            Folder = folder ?? Path.Combine(Application.persistentDataPath, "Saves");
        }

        public string Folder { get; }

        public bool Exists(string key) => File.Exists(PathOf(key, Extension)) || File.Exists(PathOf(key, TempExtension));

        public bool TryRead(string key, out string data)
        {
            foreach (var path in new[] { PathOf(key, Extension), PathOf(key, TempExtension) })
            {
                if (!File.Exists(path))
                    continue;

                data = File.ReadAllText(path, s_encoding);
                return true;
            }

            data = null;
            return false;
        }

        public void Write(string key, string data)
        {
            Directory.CreateDirectory(Folder);
            var path = PathOf(key, Extension);
            var temp = PathOf(key, TempExtension);

            File.WriteAllText(temp, data, s_encoding);
            if (File.Exists(path))
                File.Delete(path);
            File.Move(temp, path);
        }

        public void Delete(string key)
        {
            File.Delete(PathOf(key, Extension));
            File.Delete(PathOf(key, TempExtension));
        }

        private string PathOf(string key, string extension) => Path.Combine(Folder, key + extension);
    }
}
