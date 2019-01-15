using System;
using System.IO;
using GZipLibrary;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// проверка файлов
    /// входящий файл корректный (архив, если decompress)?
    /// исходящий файл корректный (хватит ли места для записи исходящего файла)?
    /// входящий файл большой?
    /// </summary>
    public class CheckFiles
    {
        private readonly CommamdsEnum m_Command;
        public string ErrMsg { get; private set; }

        private long m_DecompressedFileLen = 0;

        public CheckFiles(CommamdsEnum mCommand)
        {
            m_Command = mCommand;
        }

        public bool Check()
        {
            // проверка файла In
            if (!CheckInFile())
            {
                return false;
            }
            

            // проверка файла Out
            return CheckOutFile();
        }

        /// <summary>
        /// файл существует?
        /// если decompress это точно GZ?
        /// файл большой?
        /// </summary>
        /// <returns></returns>
        private bool CheckInFile()
        {
            var app = AppPropertiesSingle.GetInstance();
            
            if (!File.Exists(app.InFilePath))
            {
                ErrMsg = "входящий файл не существует";
                return false;
            }

            if (m_Command == CommamdsEnum.Decompress)
            {
                // проверка что это точно GZ
                using (var reader = new BinaryReader(new FileStream(app.InFilePath, FileMode.Open, FileAccess.Read)))
                {
                    var bytes = reader.ReadBytes(10);
                    if (bytes[0]!=31 || bytes[1] != 139)
                    {
                        ErrMsg = "входящий файл не является архивом Gzip";
                        return false;
                    }
                }

                Console.WriteLine("Вычисляем размер разархивированного файла...");
                // получить общий размер распакованного файла
                m_DecompressedFileLen = 0;
                using (var reader = new FileStream(app.InFilePath, FileMode.Open, FileAccess.Read))
                {
                    while (reader.Position < reader.Length)
                    {
                        var buffer = new byte[8];
                        reader.Read(buffer, 0, 8);
                        var compressedBlockLength = BitConverter.ToInt32(buffer, 4);
                        var comressedBytes = new byte[compressedBlockLength + 1];
                        buffer.CopyTo(comressedBytes, 0);
                        reader.Read(comressedBytes, 8, compressedBlockLength - 8);
                        var blockSize = BitConverter.ToInt32(comressedBytes, compressedBlockLength - 4);
                        m_DecompressedFileLen += blockSize;
                    }
                }
            }
            
            // берем свободную оперативную память
            var freeMemory = FreeRamMemory.GetFreeRamMemoryMb();

            // если две длины файла больше чем (свободная оперативка - 10%) - тогда юзаем алгоритм больших файлов
            long fileLen;
            if (m_Command == CommamdsEnum.Decompress)
            {
                //если это разархивация - тогда смотрим по длине разархивированного файла
                fileLen = m_DecompressedFileLen + m_DecompressedFileLen/10;
            }
            else
            {
                // если это архивация - берем только входящий файл
                var fileInfo = new FileInfo(app.InFilePath);
                fileLen = fileInfo.Length;
            }

            // в Мб
            fileLen = fileLen / 1024 / 1024;

            if (fileLen * 2 > freeMemory || fileLen > 1000)
            {
                app.SetBigFile();
                Console.WriteLine("Ух ты, большой файлик, придется повозиться с диском ;)");
            }

            return true;
        }

        private bool CheckOutFile()
        {
            var app = AppPropertiesSingle.GetInstance();
            
            // можно создать файл по директории?
            try
            {
                using (File.Create(app.OutFilePath)) { }
                File.Delete(app.OutFilePath);
            }
            catch
            {
                ErrMsg = "не могу создать выходной файл, проверьте доступ";
                return false;
            }

            // если входящий файл большой - определим временный путь для частей
            var outDir = Path.GetDirectoryName(app.OutFilePath);
            if (string.IsNullOrEmpty(outDir))
            {
                ErrMsg = "не могу прочитать директорию до файла";
                return false;
            }

            if (app.IsBigFile)
            {
                var temp = Path.Combine(outDir, "GZip");
                try
                {
                    Directory.CreateDirectory(temp);
                }
                catch (Exception)
                {
                    ErrMsg = "не могу создать временный путь: возможно к диску нет доступа " + temp;
                }
                app.SetTempPath(temp);
            }
            

            // определим хватит ли места для создания файла
            if (!Path.IsPathRooted(app.OutFilePath))
            {
                ErrMsg = "необходимо указать полный путь для выходного файла";
                return false;
            }
            string disk = Path.GetPathRoot(app.OutFilePath);
            long freeSpace = GetTotalFreeSpace(disk);
            if (freeSpace < 0)
            {
                ErrMsg = "не могу прочитать свободное место на диске " + disk;
                return false;
            }

            long fileLen;
            if (m_Command == CommamdsEnum.Decompress)
            {
                fileLen = m_DecompressedFileLen;
            }
            else
            {
                var info = new FileInfo(app.InFilePath);
                fileLen = info.Length;
            }

            //добавим 5% на всякий
            fileLen += fileLen / 20;
            
            if (fileLen  > freeSpace)
            {
                ErrMsg = "недостаточно свободного места на диске " + disk;
                return false;
            }

            return true;
        }

        private static long GetTotalFreeSpace(string driveName)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName.ToUpper())
                {
                    return drive.TotalFreeSpace;
                }
            }
            return -1;
        }
    }
}
