using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace copy_app
{
    /// <summary>
    /// Реализация сервиса сравнения файлов
    /// </summary>
    public class HashComparer
    {
        public History? History { get; set; }

        public bool HasHistory { get; set; }

        public HashComparer()
        {
            var serializer = new XmlSerializer(typeof(History));
            var directoryName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "history");

            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }

            var historyDirectory = new DirectoryInfo(directoryName);
            var LastFile = historyDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly).OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

            if (LastFile is null)
                return;

            History = serializer.Deserialize(File.OpenRead(LastFile.FullName)) as History;
            HasHistory = History is not null;
        }

        /// <summary>
        /// Сравнить файлы по md5 hash-сумме
        /// </summary>
        /// <param name="firstFilePath">Путь к первому файлу</param>
        /// <param name="secondFilepath">Путь ко второму файлу</param>
        /// <returns>true/false</returns>
        public virtual async Task<bool> IsSimillarByHash(HashHistory hashFirst, string secondFilepath)
        {
            using var md5 = MD5.Create();
            using var fs2 = File.OpenRead(secondFilepath);
            var hashSecond = await md5.ComputeHashAsync(fs2);

            return hashFirst.FileHash?.SequenceEqual(hashSecond) ?? false;
        }

        /// <summary>
        /// Сравнить файлы по байтам
        /// </summary>
        /// <param name="firstFilePath">Путь к первому файлу</param>
        /// <param name="secondFilepath">Путь ко второму файлу</param>
        /// <returns>true/false</returns>
        public virtual bool IsSimillarByBytes(string firstFilePath, string secondFilepath)
        {
            return true;
        }

        public virtual async Task SaveHashHistory(IEnumerable<string> values)
        {
            var History = new History()
            {
                HashHistories = values.Select(x => new HashHistory()
                {
                    FilePath = x,
                    FileHash = MD5.Create().ComputeHash(File.OpenRead(x))
                }).ToList()
            };

            var serializer = new XmlSerializer(typeof(History));
            using var ms = new MemoryStream();
            serializer.Serialize(ms,History);
            ms.Position= 0;
            await File.WriteAllBytesAsync($"history/history{DateTime.Now:dd-MM-yyyy_ss.FFF}.xml",ms.ToArray());
        }
    }
}
