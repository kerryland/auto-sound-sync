using System.Collections.ObjectModel;
using System.Windows;
using System;
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
        
        public Options()
        {
            InitializeComponent();
            
            _synchronizers = new ObservableCollection<string>();
            _synchronizers.Add("AuxMic");
            _synchronizers.Add("SoundFingerprinting");
            
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
            this.Close();
        }

        /// <summary>
        /// Close window and save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_Save(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Save();
            this.Close();
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
    }
}
