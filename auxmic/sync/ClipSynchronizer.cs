using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using auxmic.sync;

namespace auxmic
{
    public sealed class ClipSynchronizer
    {
        public Clip Master { get; set; }

        public ObservableCollection<Clip> MasterClips { get; set; }
        public ObservableCollection<Clip> LQClips { get; set; }

        private Task _loadMasterTask;
        private Task _processMasterTask;

        private TaskScheduler _taskScheduler = TaskScheduler.Current;
        
        public ClipSynchronizer()
        {
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

            this.Master = new Clip(masterFilename, fingerprinter);

            // disable export button for high quality audio source
            this.Master.DisplayExportControls = false;

            this.MasterClips.Add(Master);

            // может существовать раннее созданная задача, которая завершает обработку
            // (удаляет временный файл в LoadFile)
            if (_loadMasterTask != null)
            {
                _loadMasterTask.Wait();
            }

            // Используем две отдельных задачи, вместо цепочки задач.
            // Это нужно для того, чтобы:
            // 1. дождаться загрузки мастер-записи перед загрузкой LQ-файлов,
            //    т.к. при загрузке LQ-файлов может потребоваться их ресемплинг до мастер-записи,
            //    а он ещё не загрузился и свойство WaveFormat не доступно (== null)
            // 2. перед матчингом LQ-записей надо дождаться окончания хэширования мастер-записи
            _loadMasterTask = Task.Factory.StartNew(
                () => { Master.LoadFile(); },
                this.Master.CancellationTokenSource.Token, 
                TaskCreationOptions.None, 
                _taskScheduler);

            // если не удалось загрузить файл - удаляем его из коллекции
            // если формат не поддерживается, Media Foundation выкинет исключение 
            // MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED 
            var cleanupTask = _loadMasterTask.ContinueWith(
                (antecedent) => { CleanupMaster(); },
                /* отмену не учитываем */
                System.Threading.CancellationToken.None,
                /* выполняем только при ошибке */
                TaskContinuationOptions.OnlyOnFaulted,
                /* т.к. обращаемся к коллекции MasterClips созданной в потоке UI,
                   то используем не _taskScheduler */
                TaskScheduler.FromCurrentSynchronizationContext());

            _processMasterTask = _loadMasterTask.ContinueWith(
                (antecedent) => { Master.CalcHashes(); },
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
                throw new ApplicationException(String.Format("Cannot add file '{0}' from auxmic temp folder. Please, use other folder for source files.", Path.GetFileName(LQfilename)));
            }

            // ждём загрузки мастер-записи, т.к. для ресемплинга нам нужно знать его WaveFormat
            _loadMasterTask.Wait();

            Clip clip = new Clip(LQfilename, this.Master.Fingerprinter, this.Master.WaveFormat)
            {
                // enable export button for clip
                DisplayExportControls = true
            };

            this.LQClips.Add(clip);

            Task loadFileTask = Task.Factory.StartNew(
                    () => { clip.LoadFile(); },
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
                    (antecedent) => { clip.CalcHashes(); },
                    clip.CancellationTokenSource.Token,
                    TaskContinuationOptions.LongRunning,
                    _taskScheduler)
                .ContinueWith(
                    (antecedent) => 
                    {
                        // ждём завершения хэширования мастер-записи
                        _processMasterTask.Wait();

                        if (_processMasterTask.IsCanceled)
                        {
                            return;
                        }

                        // запускаем синхронизацию
                        clip.Sync(this.Master);
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
