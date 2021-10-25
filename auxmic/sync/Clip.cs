using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using auxmic.sync;

using WaveFormat = NAudio.Wave.WaveFormat;

namespace auxmic
{
    public sealed class Clip : INotifyPropertyChanged
    {
        // HRESULT: 0xC00D36C4 (-1072875836)
        private const int MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED = -1072875836;

        #region PROPERTIES & FIELDS

        private readonly IFingerprinter _fingerprinter;

        public IFingerprinter Fingerprinter => _fingerprinter;

        /// <summary>
        /// Формат мастер-записи к которому надо ресемплировать остальные файлы для дальнейшей работы с ними.
        /// </summary>
        internal WaveFormat _resampleFormat;

        internal CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        /// <summary>
        /// Звуковой файл (с заголовком и методом чтения данных)
        /// </summary>
        internal SoundFile SoundFile { get; set; }

        private string _filename;
        /// <summary>
        /// Полное имя файла
        /// </summary>
        public string Filename
        {
            get { return _filename; }
            set
            {
                _filename = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Filename"));
            }
        }

        private string _displayname;
        /// <summary>
        /// Отображаемое имя файла (Filename без пути)
        /// </summary>
        public string DisplayName
        {
            get
            {
                if (_displayname == null)
                {
                    _displayname = Path.GetFileName(_filename);
                }

                return _displayname;
            }

            set
            {
                _displayname = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DisplayName"));
            }
        }

        public long Length()
        {
            return SoundFile.Length;
        }

        private int _maxProgressValue;
        /// <summary>
        /// Максимальное значения прогресса.
        /// ВНИМАНИЕ: на разных этапах обработки файла это значение меняется.
        /// Сначала отображает максимальное значение прогресса при расчёте хэшей,
        /// затем отображает значение для синхронизации.
        ///
        /// Maximum progress values.
        /// ATTENTION: this value changes at different stages of file processing.
        /// First displays the maximum progress value when calculating hashes,
        /// then displays the value for synchronization.
        ///
        /// </summary>
        public int MaxProgressValue
        {
            get { return _maxProgressValue; }
            set
            {
                _maxProgressValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("MaxProgressValue"));
            }
        }

        private int _progressValue;
        /// <summary>
        /// Текущее значение прогресса.
        /// ВНИМАНИЕ: отображает сначала процесс расчёта хэшей, затем матчинга (синхронизации)
        /// </summary>
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                _progressValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ProgressValue"));
            }
        }

        private bool _isLoading;
        /// <summary>
        /// WAV извлекается из медиа-файла и сохраняется во временной директории.
        /// </summary>
        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsLoading"));
            }
        }

        private bool _isMatching;
        /// <summary>
        /// Синхронизация в процессе выполнения?
        /// Это свойство потребовалось для отображения прогресса выполнения синхронизации 
        /// другим цветом на одном и том же элементе ProgressBar. Изначально для выбора
        /// другого цвета отслеживалось свойство IsHashed, но тогда после хэширования 
        /// прогресс показывал 100% и менял цвет, в то время как требовалось менять цвет
        /// только с началом синхронизации.
        ///
        /// Synchronization in progress?
        /// This property was required to display the sync progress in a different color on the same ProgressBar.
        /// Initially, to select a different color, the IsHashed property was monitored, but then after hashing
        /// the progress showed 100% and changed color, while it was required to change the color only with the
        /// start of synchronization.
        /// </summary>
        public bool IsMatching
        {
            get { return _isMatching; }
            set
            {
                _isMatching = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMatching"));
            }
        }

        private bool _isMatched;
        /// <summary>
        /// Файл уже синхронизирован?
        /// </summary>
        public bool IsMatched
        {
            get { return _isMatched; }
            set
            {
                _isMatched = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsMatched"));
            }
        }

        private bool _isHashed;
        /// <summary>
        /// Хэши для файла посчитаны?
        /// </summary>
        public bool IsHashed
        {
            get { return _isHashed; }
            set
            {
                _isHashed = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsHashed"));
            }
        }

        private Object _hashes;
        /// <summary>
        /// Хэши для файла
        /// </summary>
        internal Object Hashes
        {
            get
            {
                if (_hashes == null)
                {
                    CalcHashes();
                }

                return _hashes;
            }

            private set
            {
                _hashes = value;

                this.IsHashed = true;

                // raise event if not canceled
                if (!IsCanceled) OnHashed(null);
            }
        }

        /// <summary>
        /// Количество сэмплов данных
        ///
        /// Number of data samples
        /// </summary>
        internal int DataLength
        {
            get
            {
                return (this.SoundFile != null) ? this.SoundFile.DataLength : 0;
            }
        }

        private ClipMatch _matchResult;

        public ClipMatch MatchResult
        {
            get
            {
                return _matchResult;
            }
        }
    

        private TimeSpan _offset;

        /// <summary>
        /// Смещение файла относительно мастер-записи
        /// </summary>
        public TimeSpan Offset
        {
            get
            {
                return _offset;
            }

            set
            {
                _offset = value;

                OnPropertyChanged(new PropertyChangedEventArgs("Offset"));

                this.IsMatched = true;

                // raise event if not canceled
                if (!IsCanceled) OnSynced(null);
            }
        }

        private bool IsCanceled { get; set; }

        /// <summary>
        /// Заголовок с форматом данных. Свойство позволяет не делать публичным SoundFile.
        /// </summary>
        public WaveFormat WaveFormat
        {
            get
            {
                return this.SoundFile.WaveFormat;
            }
        }

        /// <summary>
        /// Controls if there will be button to export synchronized result.
        /// For hiqh quality source export should be disabled.
        /// </summary>
        public bool DisplayExportControls { get; internal set; }
        #endregion

        #region EVENTS
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, e);
            }
        }

        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        public void ReportProgress(int progress)
        {
            if (this.IsMatching)
            {
                //? this.ProgressValue = Interlocked.Increment(ref _progressValue);
                this.ProgressValue++;
            }
            else
            {
                this.ProgressValue = progress;
            }

            EventHandler<ProgressChangedEventArgs> handler = ProgressChanged;

            if (handler != null)
            {
                handler(this, new ProgressChangedEventArgs(progress, null));
            }
        }

        public event EventHandler Hashed;
        /// <summary>
        /// Событие окончания расчёта хэшей.
        /// </summary>
        /// <param name="e"></param>
        public void OnHashed(EventArgs e)
        {
            if (Hashed != null)
            {
                Hashed(this, e);
            }
        }

        public event EventHandler Synced;
        /// <summary>
        /// Событие окончания синхронизации.
        /// </summary>
        /// <param name="e"></param>
        public void OnSynced(EventArgs e)
        {
            if (Synced != null)
            {
                Synced(this, e);
            }
        }
        #endregion

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filename">Filename to load</param>
        /// <param name="fingerprinter"></param>
        /// <param name="resampleFormat">Resample format. If not set (null) - will not resample.</param>
        internal Clip(string filename, IFingerprinter fingerprinter, WaveFormat resampleFormat = null)
        {
            this.Filename = filename;
            _fingerprinter = fingerprinter;
            this._resampleFormat = resampleFormat;
            SetProgressMax(); // TODO: Remove?
        }

        /// <summary>
        /// Загрузка звукового файла. Файл извлекается из медиа-контейнера, ресемплируется
        /// и в виде WAV-фала сохрнаяется во временную директорию.
        /// Если формат файла не поддерживается Media Foundation выдаст исключение 
        /// COMException MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED,
        /// которое перехватывается и оборачивается в NotSupportedException.
        ///
        /// Extract audio from file and save as a .WAV into the temp folder
        /// </summary>
        internal void LoadFile()
        {
            IsLoading = true;

            try
            {
                this.SoundFile = new SoundFile(this.Filename, this._resampleFormat);
            }
            catch (COMException ex)
            {
                this.CancellationTokenSource.Cancel();

                if (ex.ErrorCode == MF_MEDIA_ENGINE_ERR_SRC_NOT_SUPPORTED)
                {
                    throw new NotSupportedException(String.Format("'{0}' not supported.", this.DisplayName), ex);
                }
            }
            catch (Exception)
            {
                this.CancellationTokenSource.Cancel();

                throw;
            }
            finally
            {
                this.IsLoading = false;
            }

            if (this.CancellationTokenSource.IsCancellationRequested)
            {
                this.Dispose();
            }
        }

        internal void SetProgressMax(Int32 masterHashLength = 0)
        {
            this.MaxProgressValue = masterHashLength;
        }

        /// <summary>
        /// Метод запуска расчёта хэшей.
        /// </summary>
        internal void CalcHashes()
        {
            SetProgressMax();

            Hashes = _fingerprinter.CreateFingerPrints(this);
        }


        /// <summary>
        /// Starts synching with master record.
        /// </summary>
        /// <param name="master">A Clip this recording will sync with</param>
        internal void Sync(Clip master)
        {
            // since synch consider that files may not completely overlap,
            // use the maximum possible length of both files
            // SetProgressMax(master.Hashes.Count+this.Hashes.Count-1);
            SetProgressMax(100);

            _matchResult = Match(master);

            if (MatchResult == null)
            {
                return;
            }
            
            this.Offset = TimeSpan.FromSeconds(MatchResult.TrackMatchStartsAt + Math.Abs(MatchResult.QueryMatchStartsAt));
        }

        /// <summary>
        /// Инициация отмены обработки файла с дальнейшим освобождением ресурсов и очисткой кэша.
        /// </summary>
        internal void Cancel()
        {
            // инициируем запрос на отмену задачи
            this.CancellationTokenSource.Cancel();

            // выставлем свойство для отображения в GUI
            this.IsCanceled = true;

            // останавливаем обработку (расчёт хэшей и синхронизацию),
            // удаляем закэшированные данные, если есть,
            // удаляем временную wav-копию
            this.Dispose();
        }

        /// <summary>
        /// Метод остановки всех обработок. Освобождает ресурсы. Удаляет кэш. Удаляет временную копию.
        /// </summary>
        internal void Dispose()
        {
            // освобождаем ридер
            if (this.SoundFile != null) this.SoundFile.Dispose();

            // удаляем закэшированные данные, если есть
            _fingerprinter.Cleanup(this);
        }

        /// <summary>
        /// Матчинг (синхронизация) файла.
        /// Synchronise the clip with master
        /// </summary>
        /// <param name="master"></param>
        /// <returns></returns>
        private ClipMatch Match(Clip master)
        {
            if (master == null)
            {
                throw new ApplicationException("No master file specified.");
            }

            if (!master.IsHashed)
            {
                throw new ApplicationException(String.Format("Master file '{0}' has not processed yet.", master.Filename));
            }

            if (!this.IsHashed)
            {
                throw new ApplicationException(String.Format("File '{0}' has not processed yet.", this.Filename));
            }

            // для отображения прогресса другим цветом следим за этим свойством
            this.IsMatching = true;
            this.ProgressValue = 0;

            this.CancellationTokenSource.Token.ThrowIfCancellationRequested();

            return _fingerprinter.matchClips(master, this);
        }
    }
}
