using System;
using System.Diagnostics;
using System.IO;
using ZipUnzipThreadProject.ParsingCommands;

namespace ZipUnzipThreadProject
{
    public static class Program
    {
        static void Main(string[] args)
        {
            // поговорим с юзером, чтобы узнать что он хочет, если это нужно (уже есть входящие параметры)
            var command = DialogWithUser.GoDialog(args);
            
            // обработка входящих команд 
            if (command == CommamdsEnum.Exit)
            {
                Console.WriteLine("До свидания! Приходите ещё!");
                return;
            }

            var appProp = AppPropertiesSingle.GetInstance();
            if (!File.Exists(appProp.InFilePath))
            {
                Console.WriteLine("Ошибка: входящий файл не корректен!");
                return;
            }
            
            // таймер процесса
            var sw = new Stopwatch();
            sw.Start();

            // выбор стратегии в зависимости от команды
            ICutting cutFile;
            IArchiveProcessing archiveProcess;

            switch (command)
            {
                case CommamdsEnum.Compress:
                    cutFile = new CutInPiecesNormal();
                    archiveProcess = new ProcessPacking();
                    break;
                case CommamdsEnum.Decompress:
                    cutFile = new CutInPiecesCompressed();
                    archiveProcess = new ProcessUnPacking();
                    break;
                default:
                    Console.WriteLine("До свидания! Приходите ещё!");
                    return;
            }

            var fassade = new LogicFassade(cutFile, archiveProcess);

            Console.WriteLine("Разрежем файл на кусочки...");
            fassade.CutInPieces();
            var ts = sw.Elapsed;
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine(elapsedTime);

            // упаковка/распаковка кусочков
            Console.WriteLine("Работа с архивацией/разархивацией кусочков...");
            fassade.ArchiveProcess();
            ts = sw.Elapsed;
            elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine(elapsedTime);

            // собрать файл
            Console.WriteLine("Собираем файл после процесса...");
            fassade.BringUpFile();

            // таймер: диагностика выполнения
            Console.WriteLine("Процесс успешно завершен. Диагностика выполнения:");
            sw.Stop();
            ts = sw.Elapsed;
            elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine("Прошло времени " + elapsedTime);
            Console.ReadKey();
        }
    }
}
