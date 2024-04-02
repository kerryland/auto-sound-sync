using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using auxmic.sync;
using auxmic.editorExport;
using auxmic.logging;
using auxmic.mediaUtil;
using auxmic.wave;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, AuxMicLog
    {
        private ClipSynchronizer _clipSynchronizer;
        private RollingLogFile _rollingLogFile;
        private FFmpegTool _ffmpegTool;

        public MainWindow()
        {
            InitializeComponent();
            SetupUnhandledExceptionHandling();
            SetupLogging();
        }

        private void SetupLogging()
        {
            // Keep no more than 2 log files each 5mb large
            int MaxLogCount = 2;
            int MaxLogSize = 1024 * 1024 * 5;
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appName = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

            _rollingLogFile = new RollingLogFile(folder, appName, MaxLogCount, MaxLogSize);
        }

        private void SetupUnhandledExceptionHandling()
        {
            // Catch exceptions from all threads in the AppDomain.
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                Exception e = args.ExceptionObject as Exception;
                ShowUnhandledException(e, "AppDomain.CurrentDomain.UnhandledException");
                if (args.IsTerminating)
                {
                    MessageBox.Show(
                        "A fatal error has occured and the application must terminate: " +
                        Environment.NewLine + e?.Message + ": " + Environment.NewLine + e?.StackTrace,
                        "Application Closing", MessageBoxButton.OK);                    
                }
            };
            // Catch exceptions from each AppDomain that uses a task scheduler for async operations.
            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.SetObserved();
                ShowUnhandledException(args.Exception, "TaskScheduler.UnobservedTaskException");
            };
            // Catch exceptions from a single specific UI dispatcher thread.
            Dispatcher.UnhandledException += (sender, args) =>
            {
                // If we are debugging, let Visual Studio handle the exception and take us to the code that threw it.
                if (!Debugger.IsAttached)
                {
                    args.Handled = true;
                    ShowUnhandledException(args.Exception, "Dispatcher.UnhandledException");
                }
            };
        }

        void ShowUnhandledException(Exception e, string unhandledExceptionType)
        {
            try
            {
                Log($"An Unexpected {unhandledExceptionType} Error Occurred: {e?.Message}", e);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Failed to log exception: " + e + " due to " + exception);
            }
        }
        
        private void MasterPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                SetMaster(files);
            }
        }

        private void SetMaster(string[] files)
        {
            if (files.Length == 0) return;

            try
            {
                var fingerprinter = chooseFingerprinter();
                _clipSynchronizer.SetMaster(files[0], fingerprinter);
    
                Configure();
            }
            catch (Exception ex)
            {
                Log("ERROR: " + ex.Message, ex);
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        private static IFingerprinter chooseFingerprinter()
        {
            IFingerprinter fingerprinter;

            switch (Properties.Settings.Default.SYNCHRONIZER)
            {
                case "AuxMic":
                    fingerprinter = new AuxMicFingerprinter();
                    break;
                case "Emy":
                    fingerprinter = new EmyFingerprinter();
                    break;
                case "SoundFingerprinting":
                    fingerprinter = new SoundFingerprinter();
                    break;
                default:
                    throw new ApplicationException($"SYNCHRONIZER property has illegal value {Properties.Settings.Default.SYNCHRONIZER}");
            }

            return fingerprinter;
        }

        private void Configure()
        {
            // TODO: Do some proper dependency injection
            _ffmpegTool = new FFmpegTool(this, Properties.Settings.Default.FFMPEG_EXE_PATH);
            
            FFmpegTool.PathToFFmpegExe = Properties.Settings.Default.FFMPEG_EXE_PATH;
            FileToWaveStream.PathToFFmpegExe = Properties.Settings.Default.FFMPEG_EXE_PATH;
            FileToWaveFile.FFmpegTool = _ffmpegTool;
            FileToWaveStream.Log = this;
            FingerprintStreamProvider.Log = this;

            switch (Properties.Settings.Default.WAVE_PROVIDER)
            {
                case "NAudio":
                    AuxMicFingerprinter.FingerprintStreamProvider = new NaudioWavefile();
                    break;
                case "FFMpeg":
                    AuxMicFingerprinter.FingerprintStreamProvider = new FFmpegWaveFile();
                    break;
                case "Pipe":
                    AuxMicFingerprinter.FingerprintStreamProvider = new PipedWaveProvider();
                    break;
                default:
                    throw new ApplicationException(
                        $"WAVE_PROVIDER has an illegal value: {Properties.Settings.Default.WAVE_PROVIDER}");
            }
        }
        
        
        private void LQItems_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                AddLQ(files);
            }
        }

        private void AddLQ(string[] files)
        {
            if (files.Length == 0) return;

            foreach (var file in files)
            {
                try
                {
                    _clipSynchronizer.AddLQ(file);
                }
                catch (Exception ex)
                {
                    Log("Failed to add file: " + ex.Message, ex);
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _clipSynchronizer = new ClipSynchronizer(this);

            // Очищаем списки, т.к. для отображения разметки в конструкторе форм добавлена строка
            HQItems.Items.Clear();
            LQItems.Items.Clear();
            Logging.Items.Clear();
            Log($"Log file at {_rollingLogFile.LogFilename}");
            Log("Welcome to Auto Sound Sync. Please select files to synchronize");

            HQItems.ItemsSource = _clipSynchronizer.MasterClips;
            LQItems.ItemsSource = _clipSynchronizer.LQClips;

            //CollectionViewSource viewSource = new CollectionViewSource();
            //viewSource.Source = _clipSynchronizer.LQClips;
            ////SortDescription sorting = new SortDescription("Offset", ListSortDirection.Ascending);
            ////sorting.IsLiveSortingRequested = true; // ! .NET Framework 4.5 feature!
            //viewSource.SortDescriptions.Add(new SortDescription("Offset", ListSortDirection.Ascending));
            //LQItems.ItemsSource = viewSource.View;
        }

        private void cmdRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            Button removeButton = (Button)sender;

            Clip clipToCancel = (Clip)removeButton.Tag;

            _clipSynchronizer.Cancel(clipToCancel);
        }

        /// <summary>
        /// Show context menu on left click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdExportButton_Click(object sender, RoutedEventArgs e)
        {
            Button exportButton = (Button)sender;

            exportButton.ContextMenu.PlacementTarget = exportButton;
            exportButton.ContextMenu.Placement = PlacementMode.Bottom;
            exportButton.ContextMenu.StaysOpen = true;
            exportButton.ContextMenu.IsOpen = true;
        }

        /// <summary>
        /// Export synchronized audio
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdExportMatch_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            Clip clip = (Clip)mi.DataContext;

            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "WAV file|*.wav",
                DefaultExt = ".wav",
                FileName = Path.GetFileNameWithoutExtension(clip.DisplayName)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                ExportClip(clip, saveFileDialog.FileName, false);
            }
        }

        /// <summary>
        /// Export media with synchronized audio (FFmpeg)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmdExportMediaWithSynchronizedAudio_Click(object sender, RoutedEventArgs e)
        {
            // check if there FFmpeg installed
            string ffmpegExePath = Properties.Settings.Default.FFMPEG_EXE_PATH;
            if (String.IsNullOrEmpty(ffmpegExePath))
            {
                MessageBox.Show("Full path to FFmpeg executable file `ffmpeg.exe` not set. Open `File-Options` dialog to set it.", "FFmpeg not set", MessageBoxButton.OK);
                return;
            }

            if (!File.Exists(ffmpegExePath))
            {
                MessageBox.Show("Full path to FFmpeg executable file `ffmpeg.exe` not found. Open `File-Options` dialog to set it.", "FFmpeg not found", MessageBoxButton.OK);
                return;
            }

            MenuItem mi = (MenuItem)sender;

            Clip clip = (Clip)mi.DataContext;

            string ext = Path.GetExtension(clip.Filename);
            
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = $"{ext}|*{ext}",
                DefaultExt = ext,
                FileName = Path.GetFileNameWithoutExtension(clip.Filename) + "_synced"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var targetFilePath = saveFileDialog.FileName;
                ExportClip(clip, targetFilePath, true);
            }
        }

        private void ExportClip(Clip clip, string targetFilePath, bool video)
        {
            var durationLQ = (clip.Length() / clip.WaveFormat.AverageBytesPerSecond) - clip.MatchResult.QueryMatchStartsAt;
            var durationHQ = (_clipSynchronizer.Master.Length() / clip.WaveFormat.AverageBytesPerSecond) -
                             clip.MatchResult.TrackMatchStartsAt;
            var duration = Math.Min(durationHQ, durationLQ);

            // export media
            MediaExporter ffmpeg = new MediaExporter(_ffmpegTool);

            ffmpeg.Export(
                video,
                clip.Filename,
                _clipSynchronizer.Master.Filename,
                clip.MatchResult.QueryMatchStartsAt,
                clip.MatchResult.TrackMatchStartsAt,
                targetFilePath,
                duration);

            Log($"Clip exported to {targetFilePath}");
        }

        private void cmd_AddMaster(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            SetMaster(PickFiles());
        }

        private void cmd_AddLQ(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            AddLQ(PickFiles());
        }

        private string[] PickFiles()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All media files|*.wav;*.mp3;*.3g2;*.3gp;*.3gp2;*.3gpp;*.aac;*.adts;*.avi;*.asf;*.wma;*.wmv;*.m4a;*.m4v;*.mov;*.mp4;*.m2ts;*.mts;*.mpg|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = true;

            string[] result = {};

            if (openFileDialog.ShowDialog() == true)
            {
                result = openFileDialog.FileNames;
            }

            return result;
        }

        private void cmd_About(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            About about = new About();
            about.Owner = this;
            about.ShowDialog();
        }

        /// <summary>
        /// Open Options window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmd_Options(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            Options options = new Options();
            options.Owner = this;
            if (options.ShowDialog() == true)
            {
                Log("Changing Options Resets the UI");
                _clipSynchronizer.ClearCache();
                Configure();    
            }
        }
        
        private void CtrlCCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            ListView lb = (ListView)(sender);
            SetClipboard(lb.SelectedItem);
        }

        private void RightClickCopyCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            SetClipboard(((MenuItem)sender).DataContext);
        }

        private void SetClipboard(Object selected)
        {
            // Specifically using this Clipboard class, and SetDataObject, as System.Windows.Clipboard
            // has no retry logic and we were regularly getting "OpenClipboard Failed (0x800401D0 (CLIPBRD_E_CANT_OPEN))"
            if (selected != null) System.Windows.Forms.Clipboard.SetDataObject(selected.ToString());
        }

        private void cmd_ExportFinalCutPro(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            string projectFilename = this._clipSynchronizer.Master.Filename + "_fcp7.xml";
            using StreamWriter sw = new StreamWriter(projectFilename);
            
            FinalCutProExporter exporter = new FinalCutProExporter(new MediaTool());
            
            exporter.Export(this._clipSynchronizer.Master,
                            this._clipSynchronizer.LQClips, sw);
            
            Log("Final Cut Pro 7 Project file written to...");
            Log(projectFilename);
        }
        
        private void cmd_OpenCacheFolder(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(_clipSynchronizer.GetTempPath());
        }

        private void cmd_ClearCache(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("All files will be closed. Continue anyway?", "Clear cache", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.OK)
            {
                _clipSynchronizer.ClearCache();
            }
        }

        public void Log(string message, Exception e = null)
        {
            try
            {
                // Update the UI
                this.Dispatcher.Invoke(() =>
                {
                    ScrollingListBoxAppender lba = new ScrollingListBoxAppender(Logging);
                    lba.Add(message);
                    if (e != null)
                    {
                        lba.Add(e.Message);
                    }
                    lba.Flush();
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to display error " + message + " due to " + ex);
            }

            try
            {
                _rollingLogFile.Log(message, e);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to log error " + message + " due to " + ex);
            }
        }
    }

    class ScrollingListBoxAppender
    {
        private readonly ListView _listView;
        private static string guid = Guid.NewGuid().ToString();
        private List<string> msgs = new List<string>();
        
        public ScrollingListBoxAppender(ListView listView)
        {
            _listView = listView;
        }

        public void Add(string message)
        {
            msgs.Add(message);
        }

        public void Flush()
        {
            for (int i = 0; i < msgs.Count - 1; i++)
            {
                _listView.Items.Add(msgs[i]);
            }

            var location = _listView.Items.Add(guid);
            _listView.ScrollIntoView(guid); // we need a unique value to scroll to, otherwise we might scroll to an earlier appearance of the message
            _listView.Items[location] = msgs[msgs.Count-1]; // replace the guid with the text we actually want to appear.
        }
    }
}
