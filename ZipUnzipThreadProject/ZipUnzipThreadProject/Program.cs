using System;
using System.Diagnostics;
using System.IO;
using ZipUnzipThreadProject.ParsingCommands;

namespace ZipUnzipThreadProject
{
    public class Program
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

            if (!File.Exists(AppPropertiesSingle.GetInstance().InFilePath))
            {
                Console.WriteLine("Ошибка: входящий файл не корректен!");
                return;
            }
            
            // таймер процесса
            var sw = new Stopwatch();
            sw.Start();
            
            Console.WriteLine("Разрежем файл на кусочки...");
            // разрежем файл на кусочки
            var inFileQueue = new QueueOfParts();
            var cut = new CutUpTheFile(AppPropertiesSingle.GetInstance().InFilePath, inFileQueue);
            cut.Cut();
            var ts = sw.Elapsed;
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine(elapsedTime);


            // запуск обнаруженной команды
            // упаковка/распаковка кусочков
            var proccesedQueue = new QueueOfParts();
            ProcessPackingAbstract pack;
            if (command == CommamdsEnum.Compress)
            {
                Console.Write("Процесс архивации...");
                pack = new PackPieces(inFileQueue.PieceList, proccesedQueue);
            }
            else
            {
                Console.Write("Процесс разархивации...");
                pack = new UnpackPieces(inFileQueue.PieceList, proccesedQueue);
            }
            pack.ProcessPacking();
            ts = sw.Elapsed;
            elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine(elapsedTime);

            // собрать файл
            Console.WriteLine("Собираем файл после процесса...");
            var bringTogether = new BringTogether(proccesedQueue.PieceList, AppPropertiesSingle.GetInstance().OutFilePath);
            bringTogether.Collect();

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
