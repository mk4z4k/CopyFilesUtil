using System;
using NLog;
using NLog.Config;

namespace CopyUtility
{
    internal class Program
    {
        static void Main(string[] args)
        {
            LogManager.Configuration = new XmlLoggingConfiguration("NLog.config");
            if (args.Length == 0)
            {
                HelpService.ShowMessage("Не передан параметр запуска. Для подсчета объема файлов передайте параметр = 1. Для копирование = 2");
                return;
            }

            int param;
            try
            {
                param = int.Parse(args[0]);
                if (param != 1 && param != 2)
                {
                    throw new Exception();
                }
            }
            catch 
            {
                HelpService.ShowMessage("Передан неверный параметр.");
                return;
            }

            StartClass start = new StartClass();
            start.Start(param);
        }
    }
}
