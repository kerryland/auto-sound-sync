using System.Diagnostics;
using System.Windows;
using System;
using System.Reflection;

namespace auxmic.ui
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void btn_OK(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ProductName.Text = String.Format("ASS v.{0}", Assembly.GetExecutingAssembly().GetName().Version);
        }
    }
}
