using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using System.Windows.Data;
using System.ComponentModel;
using System;
using System.Windows.Controls.Primitives;
using auxmic.sync;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ClipSynchronizer _clipSynchronizer;

        public MainWindow()
        {
            InitializeComponent();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
            }
        }

        private static IFingerprinter chooseFingerprinter()
        {
            IFingerprinter fingerprinter;

            if ("AuxMic" == Properties.Settings.Default.SYNCHRONIZER)
            {
                fingerprinter = new AuxMicFingerprinter();
            }
            else
            {
                fingerprinter = new SoundFingerprinter();
            }

            return fingerprinter;
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
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _clipSynchronizer = new ClipSynchronizer();

            // Очищаем списки, т.к. для отображения разметки в конструкторе форм добавлена строка
            HQItems.Items.Clear();
            LQItems.Items.Clear();

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
                _clipSynchronizer.Save(clip, saveFileDialog.FileName);
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
            else if (!File.Exists(ffmpegExePath))
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
                var duration = (clip.Length() / clip.WaveFormat.AverageBytesPerSecond) - clip.MatchResult.QueryMatchStartsAt;
                
                // export media
                FFmpegTool ffmpeg = new FFmpegTool(ffmpegExePath);
                ffmpeg.Export(
                    clip.Filename, 
                    _clipSynchronizer.Master.Filename,
                    clip.Offset,
                    saveFileDialog.FileName,
                    duration);
            }

            //MessageBox.Show("Synced video exported.", "File exported", MessageBoxButton.OK);
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
            /* Supported Media Formats in Media Foundation
             * http://msdn.microsoft.com/ru-ru/library/windows/desktop/dd757927(v=vs.85).aspx */
            openFileDialog.Filter = "All media files|*.wav;*.mp3;*.3g2;*.3gp;*.3gp2;*.3gpp;*.aac;*.adts;*.avi;*.asf;*.wma;*.wmv;*.m4a;*.m4v;*.mov;*.mp4;*.m2ts;*.mts|All Files (*.*)|*.*";
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
            options.ShowDialog();
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
    }
}
