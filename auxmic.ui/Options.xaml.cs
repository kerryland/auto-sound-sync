using System.Collections.ObjectModel;
using System.Windows;
using System;
using System.Windows.Controls;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    ///
    ///  TODO: Disallow changing of SYNCHRONIZER if the master clip has been set
    public partial class Options : Window
    {
        private ObservableCollection<string>_synchronizers;

        public ObservableCollection<string> Synchronizers
        {
            get => _synchronizers;
            set => _synchronizers = value;
        }
    
        public String Synchronizer
        {
            get { return Properties.Settings.Default.SYNCHRONIZER;  }
            set { Properties.Settings.Default.SYNCHRONIZER = value; }
        }

        public Boolean ExportSecondaryAudio
        {
            get { return Properties.Settings.Default.EXPORT_SECONDARY_AUDIO;  }
            set { Properties.Settings.Default.EXPORT_SECONDARY_AUDIO = value; }
        }

       
        public Boolean EnableWaveProviders
        {
            get {
                return Properties.Settings.Default.SYNCHRONIZER == "AuxMic";
            }
        }

        private ObservableCollection<string> _waveProviders;

        public ObservableCollection<string> WaveProviders
        {
            get => _waveProviders;
            set => _waveProviders = value;
        }
    
        public String WaveProvider
        {
            get { return Properties.Settings.Default.WAVE_PROVIDER;  }
            set { Properties.Settings.Default.WAVE_PROVIDER = value; }
        }
        public Options()
        {
            InitializeComponent();
            
            _synchronizers = new ObservableCollection<string>();
            _synchronizers.Add("AuxMic");
            _synchronizers.Add("SoundFingerprinting");
            _synchronizers.Add("Emy");

            _waveProviders = new ObservableCollection<string>();
            _waveProviders.Add("NAudio");
            _waveProviders.Add("Pipe");
            _waveProviders.Add("FFMpeg");

            // var vov = new ObservableCollectionPropertyNotify<>()

            DataContext = this;
        }
        
        /// <summary>
        /// Close window without saving the settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Cancel(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Reload();
            DialogResult = false;
        }

        /// <summary>
        /// Close window and save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Save(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            DialogResult = true;
        }

        /// <summary>
        /// Prompts to choose FFmpeg executable file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_OpenFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "FFmpeg executable|ffmpeg.exe|All Files (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = false;

            if (openFileDialog.ShowDialog() == true)
            {
                // change settings but not save yet
                Properties.Settings.Default.FFMPEG_EXE_PATH = openFileDialog.FileName;
            }
        }

        private void Synchronizer_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
