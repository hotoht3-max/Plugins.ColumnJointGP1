using System;
using System.Collections.Generic;
using System.Globalization;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class BoltDistanceParser
    {
        /// <summary>
        /// Парсит строку текловского формата (например, "50 2*40 60") в список чисел.
        /// </summary>
        public static List<double> Parse(string input)
        {
            var result = new List<double>();
            if (string.IsNullOrWhiteSpace(input)) return result;

            // Заменяем запятые на точки для защиты от региональных настроек Windows
            input = input.Replace(',', '.');
            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                if (part.Contains("*"))
                {
                    var subParts = part.Split('*');
                    if (subParts.Length == 2 &&
                        int.TryParse(subParts[0], out int count) &&
                        double.TryParse(subParts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        for (int i = 0; i < count; i++)
                        {
                            result.Add(val);
                        }
                    }
                }
                else
                {
                    if (double.TryParse(part, NumberStyles.Any, CultureInfo.InvariantCulture, out double val))
                    {
                        result.Add(val);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Возвращает сумму всех шагов (для вычисления общего габарита болтовой группы).
        /// </summary>
        public static double GetTotalDistance(string input)
        {
            double total = 0;
            var distances = Parse(input);
            foreach (var val in distances)
            {
                total += val;
            }
            return total;
        }
    }
}