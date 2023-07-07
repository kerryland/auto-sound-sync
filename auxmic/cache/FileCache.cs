using System;
using System.Linq;
using System.Text;
using System.IO;
using MsgPack.Serialization;
using System.Security.Cryptography;

namespace auxmic
{
    /// <summary>
    /// Custom file cache implementation.
    /// Writes and reads object from/to CacheRootPath.
    /// </summary>
    internal sealed class FileCache
    {
        private static string _cacheRootPath;

        public static string TempExtension
        {
            get
            {
                return "tmp";
            }
        }

        /// <summary>
        /// Temp directory name. Formed as %temp%\auxmix_temp
        /// </summary>
        internal static string CacheRootPath
        {
            get
            {
                if (String.IsNullOrEmpty(_cacheRootPath))
                {
                    throw new ApplicationException("CacheRootPath is null or empty.");
                }

                return _cacheRootPath;
            }

            set
            {
                _cacheRootPath = value;

                DirectoryInfo cacheRoot = new DirectoryInfo(_cacheRootPath);

                // create if it doesn't exist
                if (!cacheRoot.Exists)
                    cacheRoot.Create();
            }
        }

        /// <summary>
        /// Checks if this file already exists
        /// </summary>
        /// <param name="filename">File name to check</param>
        /// <returns></returns>
        internal static bool Exists(string filename)
        {
            return File.Exists(filename);
        }

        /// <summary>
        /// Генерация имени временного файла. Формат имени временного файла: '<original_mediafile_name>.<original_mediafile_extension>.<hash>.wav'
        /// </summary>
        /// <param name="filename">Полный путь к обрабатываемому медиафайлу.</param>
        /// <returns></returns>
        internal static string ComposeTempFilename(string filename)
        {
            // считаем хэш по полному пути обрабатываемого файла
            // необходимо для различия извлечённых WAV-файлов во временной директории (см. #1)
            string filenameHash = Hash(filename);

            return Path.Combine(FileCache.CacheRootPath, GetExtractedWavFilename(filename, filenameHash));
        }

        static FileCache()
        {
            CacheRootPath = Path.Combine(Path.GetTempPath(), "auxmic_temp");
        }

        internal static bool Contains(string key)
        {
            return File.Exists(Path.Combine(CacheRootPath, key));
        }


        /// <summary>
        /// Gets object by key. Key is a filename.
        /// Actually it reads file <paramref name="key"/> from CacheRootPath to object.
        /// If not such file - returns null.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal static Int32[] Get(string key)
        {
            int[] cached = null;

            if (Contains(key))
            {
                using (FileStream fs = new FileStream(Path.Combine(CacheRootPath, key), FileMode.Open))
                {
	   	    var serializer = MessagePackSerializer.Get<Int32[]>();
                    cached = serializer.Unpack(fs);
                }
            }

            return cached;
        }


        /// <summary>
        /// Removes item from cache - deletes tmp-file if exists.
        /// </summary>
        /// <param name="key"></param>
        internal static void Remove(string key)
        {
            string fileToDelete = Path.Combine(CacheRootPath, key);

            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete); 
            }
        }

	internal static void Set<T>(string key, T value)
	{
	    var formatter = MessagePackSerializer.Get<T>();
            using (FileStream fs = File.Open(Path.Combine(CacheRootPath, key), FileMode.Create))
            {
                formatter.Pack(fs, value);
            }
	}

        /// <summary>
        /// Writes <paramref name="value"/> object to file with name <paramref name="key"/>
        /// in CacheRootPath directory.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal static void Set_OLD(string key, object value)
        {
            /*
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream fs = File.Open(Path.Combine(CacheRootPath, key), FileMode.Create))
            {
                formatter.Serialize(fs, value);
            }
            */
        }

        internal object this[string key]
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Clears only temp files.
        /// </summary>
        /// <returns></returns>
        internal static int Clear()
        {
            return Clear(TempExtension);
        }

        /// <summary>
        /// Clears files with certain extension.
        /// </summary>
        /// <param name="extensionToClear"></param>
        /// <returns></returns>
        internal static int Clear(string extensionToClear)
        {
            int filesDeleted = 0;

            DirectoryInfo di = new DirectoryInfo(CacheRootPath);
            FileInfo[] files = di.GetFiles("*." + extensionToClear)
                                 .Where(p => p.Extension == "." + extensionToClear).ToArray();
            foreach (FileInfo tempFile in files)
            {
                tempFile.Attributes = FileAttributes.Normal;
                File.Delete(tempFile.FullName);
                filesDeleted++;
            }

            return filesDeleted;
        }

        internal static string GetTempFilename(string filename, int sampleRate)
        {
            return String.Format("{0}.{1}.{2}",
                                Path.GetFileName(filename),
                                sampleRate,
                                FileCache.TempExtension);
        }

        private static string GetExtractedWavFilename(string filename, string hash)
        {
            return String.Format("{0}.{1}.wav",
                                Path.GetFileName(filename),
                                hash);
        }

        /// <summary>
        /// Первые 7 символов SHA1 хэша.
        /// </summary>
        /// <param name="stringToHash">Строка по которой будет посчитан хэш</param>
        /// <returns></returns>
        public static string Hash(string stringToHash)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                return BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(stringToHash))).Replace("-", "").Substring(0, 7).ToLower();
            }
        }
    }
}
