using System;
using GZipLibrary;

namespace ZipUnzipThreadProject
{
    /// <summary>
    /// класс диалога с юзверем
    /// показывает приветствие
    /// инициализирует запуск считывания параметров командной строки
    /// либо опрашивает пользователя ввести команду и пути    
    /// </summary>
    public static class DialogWithUser
    {
        public static CommamdsEnum GoDialog(string[] args)
        {
            Console.WriteLine("Программа Архиватор приветствует пользователя!");

            if (args == null || args.Length <= 0)
            {
                HelpOutput();
                var input = Console.ReadLine();
                if (string.IsNullOrEmpty(input))
                {
                    return GoDialog(args); 
                }
                args = input.Split(' ');
            }

            var cmd = GetParams(args);
            if (cmd != CommamdsEnum.Help)
            {
                return cmd;
            }

            return GoDialog(new string[] { });
        }

        private static void HelpOutput()
        {
            Console.WriteLine("Команды:");
            Console.WriteLine("exit - для выхода");
            Console.WriteLine("compress \"путь файла для архивации\" \"путь заархивированного файла\" - для архивации");
            Console.WriteLine("decompress \"путь файла для разархивации\" \"путь разархивированного файла\" - для разархивации");
        }

        private static CommamdsEnum GetParams(string[] args)
        {
            var app = AppPropertiesSingle.GetInstance();
            var parseArgs = new ParsingInputParameters(args, app.ParamsDictionary);
            bool rez = parseArgs.Parse();
            if (!rez)
            {
                Console.WriteLine("Ошибка ввода параметров запуска Архиватора!");
                return CommamdsEnum.Help;
            }

            app.SetInFilePath(parseArgs.InFilePath);
            app.SetOutFilePath(parseArgs.OutFilePath);
            return parseArgs.ChosenCommand;
        }
    }
}
