using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace CopyUtility
{
    public class CopyService
    {
        private const string _copiedFilePath = ".\\History\\copiedFiles.txt";

        private List<string> _dbFilesRef;
        private List<string> _copiedFiles;

        private string _destinationPath;
        private DbManager _dbManager;

        private string _initialPath;
        private int _totalCopiedCount;
        private int _currentCopiedCount;
        private long _totalSizeInKb;

        private Logger _logger;

        private CancellationTokenSource _cancelTokenSource;


        public CopyService()
        {
            _destinationPath = ConfigurationManager.AppSettings["DestinationFilePath"];
            _dbManager = new DbManager();
            _logger = LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// Начало процедуры поиска и копирования
        /// </summary>
        /// <returns></returns>
        public void StartCopyProcess()
        {
            _logger.Log(LogLevel.Info, "Подготовка к копированию.");
            PrepareForCopy();
            if (_dbFilesRef == null || _dbFilesRef.Count == 0)
            {
                FindFilesInDb();
                _logger.Log(LogLevel.Info, "Получены ссылки на файлы из БД.");
            }

            try
            {
                _logger.Log(LogLevel.Info, "Начало копирования.");
                _cancelTokenSource = new CancellationTokenSource();
                CancellationToken token = _cancelTokenSource.Token;
                Task copyThread = Task.Factory.StartNew(() => FindAndCopyFiles(token), token);
                Task.Run(() => StopJob(token), token);
                copyThread.Wait();
                _cancelTokenSource.Cancel();
                _logger.Log(LogLevel.Info, "Копирование завершено.");
                _logger.Log(LogLevel.Info, $"Скопировано {_totalCopiedCount} файлов, общий размер {_totalSizeInKb}");
                HelpService.ShowMessage(
                    $"Копирование завершено, за время работы утилиты скопировано {_totalCopiedCount} файлов. Размер скопированных файлов: {_totalSizeInKb} КБ");

            }
            catch (Exception e)
            {
                _cancelTokenSource.Cancel();
                HelpService.ShowMessage(
                    $"Копирование прервано из-за ошибки, скопировано {_currentCopiedCount} файлов. Размер скопированных файлов: {_totalSizeInKb} КБ");
                throw;
            }
            finally
            {
                _cancelTokenSource.Dispose();
                _cancelTokenSource = null;
            }

        }

        /// <summary>
        /// Подсчет размера файлов
        /// </summary>
        public void CalcSummaryWeight()
        {
            GetInitialPath();
            FindFilesInDb();
            _logger.Log(LogLevel.Info, "Получены ссылки на файлы из БД.");
            CalcFilesSize();
            _logger.Info($"Найдено {_totalCopiedCount} файлов, общий размер {_totalSizeInKb} КБ");
            HelpService.ShowMessage($"Подсчет завершен. Найдено {_totalCopiedCount} файлов. Размер найденных файлов: {_totalSizeInKb} КБ");
        }

        /// <summary>
        /// Подготовка перед копированим
        /// </summary>
        private void PrepareForCopy()
        {
            _currentCopiedCount = 0;
            GetInitialPath();
            //Создаем директории аналогично исходной директории
            foreach (string dirPath in Directory.GetDirectories(_initialPath, "*",
                         SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(_initialPath, _destinationPath));
            }

            Directory.CreateDirectory(".\\History");
            if (!File.Exists(_copiedFilePath))
            {
                File.Create(_copiedFilePath).Close();
            }

            if (_copiedFiles == null)
            {
                FillCopiedFiles();
                _logger.Log(LogLevel.Info, "Загружена информаци о скопированных файлах.");
            }
            else
            {
                if (_dbFilesRef != null)
                {
                    _dbFilesRef = _dbFilesRef.Except(_copiedFiles).ToList();
                }
            }
        }

        /// <summary>
        /// Заполнение уже скопированных файлов из файла _copiedFilePath
        /// </summary>
        private void FillCopiedFiles()
        {
            _copiedFiles = new List<string>();
            using (StreamReader reader = new StreamReader(_copiedFilePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    _copiedFiles.Add(line);
                }
            }
            // На всякий случай удалим последний файл, чтобы заново его скопировать
            if (_copiedFiles.Count > 0)
            {
                _copiedFiles.RemoveAt(_copiedFiles.Count - 1);
            }
        }

        /// <summary>
        /// Получаем путь хранения файлов из настроек системы
        /// </summary>
        private void GetInitialPath()
        {
            _initialPath = _dbManager.GetFilePathFromSettings();
            _logger.Log(LogLevel.Info, "Получен исходный путь к файлам.");
        }

        /// <summary>
        /// Поиск всех фалов на которые есть сслыка в бд
        /// </summary>
        private void FindFilesInDb()
        {
            _dbFilesRef = _dbManager.GetFilesReference();

            if (_copiedFiles != null && _copiedFiles.Count > 0)
            {
                _dbFilesRef = _dbFilesRef.Except(_copiedFiles).ToList();
            }
        }

        /// <summary>
        /// Ищем файлы и копируем на которые есть ссылка и сущесвуют на диске 
        /// </summary>
        private void FindAndCopyFiles(CancellationToken token)
        {
            using (StreamWriter writer = new StreamWriter(_copiedFilePath, true))
            {
                foreach (var dbFile in _dbFilesRef)
                {
                    if (!token.IsCancellationRequested)
                    {
                        if (File.Exists(_initialPath + "\\" + dbFile))
                        {
                            var fileSize = HelpService.RoundForMultiplyOfFour(new FileInfo(_initialPath + "\\" + dbFile).Length);
                            _totalSizeInKb += fileSize;
                            File.Copy(_initialPath + "\\" + dbFile, _destinationPath + "\\" + dbFile, true);
                            writer.WriteLine(dbFile);
                            _copiedFiles.Add(dbFile);
                            _currentCopiedCount++;
                            _totalCopiedCount++;
                            _logger.Log(LogLevel.Info, $"Файл {_initialPath + "\\" + dbFile} размером {fileSize} КБ скопирован.");
                        }
                    }
                }
            }

        }

        private void CalcFilesSize()
        {
            foreach (var dbFile in _dbFilesRef)
            {
                if (File.Exists(_initialPath + "\\" + dbFile))
                {
                    var fileSize = HelpService.RoundForMultiplyOfFour(new FileInfo(_initialPath + "\\" + dbFile).Length);

                    _totalSizeInKb += fileSize;
                    _logger.Log(LogLevel.Info, $"Найден файл {_initialPath + "\\" + dbFile} размером {fileSize} КБ.");
                    _totalCopiedCount++;
                }
            }
        }

        /// <summary>
        /// Отслеживание нажатия кнопки для остановки копирования
        /// </summary>
        /// <param name="token"></param>
        private void StopJob(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                HelpService.ShowMessage("Идет копирование, подождите... Для прерывания введите S");
                var ans = Console.ReadKey(true);
                if (ans.Key == ConsoleKey.S && !token.IsCancellationRequested)
                {
                    _cancelTokenSource.Cancel();
                    HelpService.ShowMessage("Копирование остановлено.");
                    _logger.Info("Копирование остановлено.");
                }
            }
        }

    }
}
