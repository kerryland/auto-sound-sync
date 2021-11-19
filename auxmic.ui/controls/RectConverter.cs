using System;
using System.Windows;
using System.Windows.Data;

namespace auxmic.ui.controls
{
    /// <summary>
    /// Dynamic text color progress bar converter
    /// https://stackoverflow.com/a/5173880/155687
    /// </summary>
    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double width = (double)values[0];
                double height = (double)values[1];
                return new Rect(0, 0, width, height);
            }
            catch (Exception)
            {
                return new Rect(0, 0, 10, 10);  // TODO: Fix properly.
            }
           
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
