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
            }

            // вычислим большой ли файл
            // берем свободную оперативную память
            var freeMemory = FreeRamMemory.GetFreeRamMemoryMb();

            // если две длины файла больше чем (свободная оперативка - 10%) - тогда юзаем алгоритм больших файлов
            var fileInfo = new FileInfo(app.InFilePath);
            var fileLen = fileInfo.Length / 1024 / 1024;

            if (fileLen * 2 <= freeMemory)
                return true;

            app.SetBigFile();
            Console.WriteLine("Ух ты, большой файлик, придется повозиться с диском ;)");

            return true;
        }

        private bool CheckOutFile()
        {
            var app = AppPropertiesSingle.GetInstance();
            if (!Path.IsPathRooted(app.OutFilePath))
            {
                ErrMsg = "необходимо указать полный путь для выходного файла";
                return false;
            }

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
            string disk = Path.GetPathRoot(app.OutFilePath);
            long freeSpace = GetTotalFreeSpace(disk);
            //Console.WriteLine(freeSpace/1024);
            if (freeSpace < 0)
            {
                ErrMsg = "не могу прочитать свободное место на диске " + disk;
                return false;
            }

            var info = new FileInfo(app.InFilePath);
            var fileLen = info.Length;
            //Console.WriteLine(fileLen*6);
            if (app.IsBigFile)
            {
                if (fileLen * 6 > freeSpace)
                {
                    ErrMsg = "недостаточно свободного места на диске " + disk;
                    return false;
                }
            }
            else
            {
                if (fileLen * 2 > freeSpace)
                {
                    ErrMsg = "недостаточно свободного места на диске " + disk;
                    return false;
                }
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
