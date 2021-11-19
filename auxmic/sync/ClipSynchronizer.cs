using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using auxmic.logging;
using auxmic.sync;

namespace auxmic
{
    public sealed class ClipSynchronizer
    {
        private readonly AuxMicLog Log;
        public Clip Master { get; set; }

        public ObservableCollection<Clip> MasterClips { get; set; }
        public ObservableCollection<Clip> LQClips { get; set; }

        private Task _loadMasterTask;
        private Task _processMasterTask;

        private TaskScheduler _taskScheduler = TaskScheduler.Current;
        private ISoundFileFactory _soundFileFactory = new SoundFileFactory();
        
        public ClipSynchronizer(AuxMicLog log)
        {
            Log = log;
            this.MasterClips = new ObservableCollection<Clip>();
            this.LQClips = new ObservableCollection<Clip>();
        }

        public void SetMaster(string masterFilename, IFingerprinter fingerprinter)
        {
            if (Path.GetDirectoryName(masterFilename) == GetTempPath())
            {
                throw new ApplicationException(String.Format("Cannot add file '{0}' from auxmic temp folder. Please, use other folder for source files.", Path.GetFileName(masterFilename)));
            }

            // если мастер уже установлен - очищаем его
            if (this.Master != null)
            {
                this.Master.Cancel();
                this.MasterClips.Clear();
            }

            this.Master = new Clip(masterFilename, fingerprinter, Log, _soundFileFactory);

            // disable export button for high quality audio source
            this.Master.DisplayExportControls = false;

            this.MasterClips.Add(Master);

            // может существовать раннее созданная задача, которая завершает обработку
            // (удаляет временный файл в LoadFile)
            if (_loadMasterTask != null)
            {
                _loadMasterTask.Wait();
                if (_loadMasterTask.Exception != null)
                {
                    Log.Log($"Failed to load master: {_loadMasterTask.Exception.Message}", _loadMasterTask.Exception);
                }
            }

            // Используем две отдельных задачи, вместо цепочки задач.
            // Это нужно для того, чтобы:
            // 1. дождаться загрузки мастер-записи перед загрузкой LQ-файлов,
            //    т.к. при загрузке LQ-файлов может потребоваться их ресемплинг до мастер-записи,
            //    а он ещё не загрузился и свойство WaveFormat не доступно (== null)
            // 2. перед матчингом LQ-записей надо дождаться окончания хэширования мастер-записи
            
            // We use two separate tasks instead of a chain of tasks. This is necessary in order to:
            // 1. wait for the master record to load before loading the LQ files. when loading LQ files,
            //    they may need to be resampled before the master record, but it has not been loaded yet
            //    and the WaveFormat property is not available (== null)
            // 2.before matching LQ records, you must wait until the master record has been hashed
            _loadMasterTask = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        Master.LoadFile();
                    }
                    catch (Exception e)
                    {
                        Log.Log($"Failed to load master file: {e.Message}", e);
                    }
                },
                this.Master.CancellationTokenSource.Token, 
                TaskCreationOptions.None, 
                _taskScheduler);

            // если не удалось загрузить файл - удаляем его из коллекции
            // если формат не поддерживается, Media Foundation выкинет исключение 
            // MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED 
            var cleanupTask = _loadMasterTask.ContinueWith(
                (antecedent) =>
                {
                    try
                    {
                        CleanupMaster();
                    }
                    catch (Exception e)
                    {
                        Log.Log($"Failed to cleanup master: {e.Message} ", e);
                    }
                },
                /* отмену не учитываем */
                System.Threading.CancellationToken.None,
                /* выполняем только при ошибке */
                TaskContinuationOptions.OnlyOnFaulted,
                /* т.к. обращаемся к коллекции MasterClips созданной в потоке UI,
                   то используем не _taskScheduler */
                TaskScheduler.FromCurrentSynchronizationContext());

            _processMasterTask = _loadMasterTask.ContinueWith(
                (antecedent) =>
                {
                    try
                    {
                        Master.CalcHashes();
                    }
                    catch (Exception e)
                    {
                        Log.Log($"{Master.DisplayName} fingerprinting failed", e);
                    }
                },
                this.Master.CancellationTokenSource.Token,
                TaskContinuationOptions.LongRunning,
                _taskScheduler);

            // очищаем коллекцию LQ-записей, т.к. сменился мастер-файл, который имеет 
            // другой формат и раннее посчитанные LQ-файлы ему могут не соответствовать -
            // требуется их повторная обработка
            CleanupLQ();
        }

        private void CleanupMaster()
        {
            // если есть доступ к App вызываем в том же потоке, что и UI
            //App.Current.Dispatcher.Invoke((Action)delegate
            //{
            //});

            if (this.Master != null) this.Master.Cancel();
            this.MasterClips.Clear();

            _loadMasterTask = null;
        }

        public void AddLQ(string LQfilename)
        {
            if (Path.GetDirectoryName(LQfilename) == GetTempPath())
            {
                throw new ApplicationException(
                    $"Cannot add file '{Path.GetFileName(LQfilename)}' from auxmic temp folder. Please, use other folder for source files.");
            }

            // ждём загрузки мастер-записи, т.к. для ресемплинга нам нужно знать его WaveFormat
            // waiting for the master record to load, because for resampling we need to know its WaveFormat
            // TODO: Is this really true?
            _loadMasterTask.Wait();
            if (_loadMasterTask.Exception != null)
            {
                throw new ApplicationException(
                    $"Problem with {this.Master.DisplayName}: {_loadMasterTask.Exception.ToString()}",
                    _loadMasterTask.Exception);
            }

            Clip clip = new Clip(LQfilename, this.Master.Fingerprinter, Log, _soundFileFactory, this.Master.WaveFormat)
            {
                // enable export button for clip
                DisplayExportControls = true
            };

            this.LQClips.Add(clip);

            Task loadFileTask = Task.Factory.StartNew(
                () =>
                {
                    try
                    {
                        clip.LoadFile();
                    }
                    catch (Exception e)
                    {
                        Log.Log($"{clip.DisplayName} failed to load", e);
                    }
                },
                    clip.CancellationTokenSource.Token,
                    TaskCreationOptions.None,
                    _taskScheduler);

            // если не удалось зугрузить файл - удаляем его из коллекции
            var cleanupTask = loadFileTask.ContinueWith(
                (antecedent) => { this.LQClips.Remove(clip); },
                /* отмену не учитываем */
                System.Threading.CancellationToken.None,
                /* выполняем только при ошибке */
                TaskContinuationOptions.OnlyOnFaulted,
                /* т.к. обращаемся к коллекции LQClips созданной в потоке UI,
                   то используем не _taskScheduler */
                TaskScheduler.FromCurrentSynchronizationContext());

            Task processTask = loadFileTask.ContinueWith(
                    (antecedent) =>
                    {
                        try
                        {
                            clip.CalcHashes();
                        }
                        catch (Exception e)
                        {
                            Log.Log($"{clip.DisplayName} fingerprinting... FAILED", e);
                        }
                    },
                    clip.CancellationTokenSource.Token,
                    TaskContinuationOptions.LongRunning,
                    _taskScheduler)
                .ContinueWith(
                    (antecedent) => 
                    {
                        // ждём завершения хэширования мастер-записи
                        _processMasterTask.Wait();

                        if (_processMasterTask.Exception != null)
                        {
                            throw new ApplicationException("Task failed", _processMasterTask.Exception);
                        }
                        if (_processMasterTask.IsCanceled)
                        {
                            return;
                        }

                        // запускаем синхронизацию
                        Log.Log($"{clip.DisplayName} synchronizing...");
                        clip.Sync(this.Master);
                        Log.Log($"{clip.DisplayName} synchronizing... Done");
                    },
                    clip.CancellationTokenSource.Token,
                    TaskContinuationOptions.LongRunning,
                    _taskScheduler);
                //.ContinueWith(
                //    (antecedent) => 
                //    {
                //        this.LQClips.OrderBy(c => c.Offset);
                //    });
        }

        public void Cancel(Clip clip)
        {
            if (this.LQClips.Contains(clip))
            {
                this.LQClips.Remove(clip);
            }
            else if (this.MasterClips.Contains(clip))
            {
                this.MasterClips.Remove(clip);

                // перед отменой мастера,
                // отменяем все LQ-задачи
                CleanupLQ();
            }

            // отменяем обработку
            clip.Cancel();
        }

        /// <summary>
        /// Очищает коллекцию lq-файлов предварительно удалив все временные файлы.
        /// </summary>
        private void CleanupLQ()
        {
            foreach (Clip сlip in this.LQClips)
            {
                сlip.Cancel();
            }

            this.LQClips.Clear();
        }

        /// <summary>
        /// Сохранение синхронизированного файла.
        /// Extract a subset of the master file to create a new audio file for "clip" named "filename".
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="filename"></param>
        public void Save(Clip clip, string filename)
        {
            if (clip.MatchResult == null)
            {
                throw new ApplicationException("Could not find a match");
            }

            this.Master.SoundFile.SaveMatch(filename, clip.MatchResult.QueryMatchStartsAt, 
                clip.MatchResult.TrackMatchStartsAt, clip.SoundFile.Length);
            
            Log.Log($"{filename} has been saved");
        }

        /// <summary>
        /// Расположение временной директории
        /// </summary>
        /// <returns></returns>
        public string GetTempPath()
        {
            return FileCache.CacheRootPath;
        }

        /// <summary>
        /// Clears temp and copied wav files.
        /// </summary>
        public void ClearCache()
        {
            CleanupMaster();
            CleanupLQ();

            // если оставались другие временные файлы - удаляем и их
            FileCache.Clear();
            FileCache.Clear("wav");
        }
    }
}
