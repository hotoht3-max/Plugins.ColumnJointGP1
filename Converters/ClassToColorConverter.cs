using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RAM.Plugins.ColumnJointGP1.Converters
{
    public class ClassToColorConverter : IValueConverter
    {
        // Стандартная палитра Tekla Structures (0-14)
        private static readonly string[] TeklaColors = new[]
        {
            "#000000", // 0: Черный
            "#999999", // 1: Серый
            "#FF0000", // 2: Красный
            "#008000", // 3: Зеленых (Темный)
            "#00008B", // 4: Синий
            "#00FFFF", // 5: Голубой
            "#FFFF00", // 6: Желтый
            "#FF00FF", // 7: Пурпурный
            "#8B4513", // 8: Коричневый
            "#FF1493", // 9: Розовый
            "#90EE90", // 10: Светло-зеленый (включая класс 94)
            "#ADD8E6", // 11: Светло-синий
            "#DDA0DD", // 12: Сиреневый
            "#FFA500", // 13: Оранжевый
            "#4B0082"  // 14: Темно-фиолетовый
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !int.TryParse(value.ToString(), out int classNum))
                return new SolidColorBrush(Colors.Transparent);

            // Математика цикличных цветов Теклы
            int colorIndex = classNum == 0 ? 0 : (classNum % 14 == 0 ? 14 : Math.Abs(classNum) % 14);

            try
            {
                var color = (Color)ColorConverter.ConvertFromString(TeklaColors[colorIndex]);
                return new SolidColorBrush(color);
            }
            catch
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}