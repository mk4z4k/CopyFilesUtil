using System;
using NLog;

namespace CopyUtility
{
    public class StartClass
    {
        public void Start(int param)
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            logger.Info("Начало работы приложения.");
            if (param == 2)
            {
                HelpService.ShowMessage("Начат поиск файлов для копирования.");
                CopyService copyService = new CopyService();
                try
                {
                    copyService.StartCopyProcess();
                    HelpService.ShowMessage("Для выхода нажмите Enter.");
                    Console.ReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\nВозникла ошибка. Подробности в файле логе.\n");
                    logger.Log(LogLevel.Error, ex);
                }
            }
            else
            {
                HelpService.ShowMessage("Начат подсчет размера файлов для копирования.");
                CopyService copyService = new CopyService();
                try
                {
                    copyService.CalcSummaryWeight();
                }
                catch (Exception e)
                {
                    Console.WriteLine("\nВозникла ошибка. Подробности в файле логе.\n");
                    logger.Error(e);
                }
                
            }
            logger.Info("Завершение работы приложения.");
        }
    }
}
