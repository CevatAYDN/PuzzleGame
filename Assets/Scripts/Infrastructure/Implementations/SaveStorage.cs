using System.IO;
using System.Text;
using PuzzleGame.Application.Interfaces;
using UnityEngine;

namespace PuzzleGame.Infrastructure.Implementations
{
    /// <summary>
    /// File-backed save storage rooted at <see cref="Application.persistentDataPath"/>.
    /// Atomic write: payload goes to a .tmp sibling, then is renamed onto the
    /// final path so a crash mid-write never leaves a half-written save.
    /// </summary>
    public sealed class SaveStorage : ISaveStorage
    {
        private const string SaveFileName = "puzzlegame_save.json";

        public string FilePath => Path.Combine(UnityEngine.Application.persistentDataPath, SaveFileName);
        public string TempPath => FilePath + ".tmp";

        public bool Exists() => File.Exists(FilePath);

        public string ReadAll() => File.ReadAllText(FilePath, Encoding.UTF8);

        public void WriteAtomic(string content)
        {
            string dir = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(TempPath, content, Encoding.UTF8);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(TempPath, FilePath);
        }

        public void Delete()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }

        public long GetSize()
        {
            try
            {
                if (File.Exists(FilePath)) return new FileInfo(FilePath).Length;
            }
            catch
            {
                return 0;
            }
            return 0;
        }
    }
}
