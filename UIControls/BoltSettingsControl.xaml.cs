using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace RAM.Plugins.ColumnJointGP1.UIControls
{
    public partial class BoltSettingsControl : UserControl
    {
        public BoltSettingsControl()
        {
            InitializeComponent();
        }
    }

    // Тот самый "переводчик" между WPF и Tekla
    public class IntToBoolConverter : IValueConverter
    {
        // Перевод из ViewModel (int) в UI (bool)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
                return intValue == 1;
            return false;
        }

        // Перевод из UI (bool) обратно во ViewModel (int)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
                return boolValue ? 1 : 0;
            return 0;
        }
    }
}