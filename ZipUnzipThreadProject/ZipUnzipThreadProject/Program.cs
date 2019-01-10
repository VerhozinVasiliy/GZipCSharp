using System;
using System.Diagnostics;
using System.IO;
using GZipLibrary;

namespace ZipUnzipThreadProject
{
    public static class Program
    {
        static void Main(string[] args)
        {
            // Установим основные параметры архиватора
            SetParams();

            // поговорим с юзером, чтобы узнать что он хочет, если это нужно (уже есть входящие параметры)
            var command = DialogWithUser.GoDialog(args);
            if (command == CommamdsEnum.Exit)
            {
                Console.WriteLine("До свидания! Приходите ещё!");
                return;
            }

            Console.WriteLine("Проверка входящих параметров...");
            var check = new CheckFiles(command);
            if (!check.Check())
            {
                Console.WriteLine("Ошибка проверки входных параметров: {0}", check.ErrMsg);
                return;
            }
            
            // таймер процесса
            var sw = new Stopwatch();
            sw.Start();

            // выбор стратегии в зависимости от команды
            var cs = new ChooseStrategy(command);
            cs.Choose();
            var fassade = new LogicFassade(cs.CutFile, cs.ArchiveProcess, cs.Collecting);
            // сообщения о прогрессе
            cs.CutFile.NotifyProgress += NotifyProgress;
            cs.ArchiveProcess.NotifyProgress += NotifyProgress;
            cs.Collecting.NotifyProgress += NotifyProgress;

            var appProp = AppPropertiesSingle.GetInstance();
            Console.WriteLine("Работаю с файлом {0}", appProp.InFilePath);
            Console.WriteLine("Разрежем файл на кусочки...");
            CleanTemp(false);
            fassade.CutInPieces();
            NotificTime(sw.Elapsed);
            GC.Collect();
            
            // упаковка/распаковка кусочков
            switch (command)
            {
                case CommamdsEnum.Compress:
                    Console.WriteLine("Архивация кусочков...");
                    break;
                case CommamdsEnum.Decompress:
                    Console.WriteLine("Разархивация кусочков...");
                    break;
                default:
                    Console.WriteLine("Идет какойто непонятный процесс...");
                    break;
            }
            fassade.ArchiveProcess();
            NotificTime(sw.Elapsed);

            // собрать файл
            Console.WriteLine("Собираем файл после процесса...");
            fassade.BringUpFile();
            Console.Write("\r");

            Console.WriteLine("Процесс успешно завершен. Диагностика выполнения:");
            sw.Stop();
            NotificTime(sw.Elapsed);
            Console.WriteLine("Убираю за собой....");
            CleanTemp();
            Console.WriteLine("Готово! Выходной файл {0}", appProp.OutFilePath);
            Console.ReadKey();
        }

        /// <summary>
        /// обработчик для показа прогресса исполнения из бизнес-логики
        /// </summary>
        /// <param name="message"></param>
        private static void NotifyProgress(string message)
        {
            Console.Write("\r");
            for (int i = 0; i < 40; i++)
            {
                Console.Write(" ");
            }
            Console.Write("\r{0}%", message);
        }

        /// <summary>
        /// показать сколько времени прошло после выполнения функции
        /// </summary>
        /// <param name="ts"></param>
        private static void NotificTime(TimeSpan ts)
        {
            Console.Write("\r");
            for (int i = 0; i < 40; i++)
            {
                Console.Write(" ");
            }
            Console.Write("\r");
            var elapsedTime = $"{ts.Hours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds / 10:00}";
            Console.WriteLine("Прошло времени " + elapsedTime);
        }

        /// <summary>
        /// зададим начальные параметры
        /// </summary>
        private static void SetParams()
        {
            var prop = AppPropertiesSingle.GetInstance();

            prop.SetBufferSize(1048576);
            
            prop.SetProcessorCount(Environment.ProcessorCount);
        }

        /// <summary>
        /// немного приберемся после выполнения
        /// </summary>
        private static void CleanTemp(bool deleteTempDir = true)
        {
            var app = AppPropertiesSingle.GetInstance();
            if (!Directory.Exists(app.TempPath))
            {
                return;
            }

            var files = Directory.GetFiles(app.TempPath);
            foreach (var file in files)
            {
                File.Delete(file);
            }

            if (deleteTempDir)
            {
                Directory.Delete(app.TempPath);
            }
        }
    }
}
