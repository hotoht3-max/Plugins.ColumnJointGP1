using RAM.Plugins.ColumnJointGP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class UdaParser
    {
        /// <summary>
        /// Разбирает строку вида "NAME1=Val1;NAME2=Val2" в список из 4 строк UDA для UI.
        /// </summary>
        public static List<UdaRow> Parse(string udaString, bool[] states)
        {
            var rows = new List<UdaRow>
            {
                new UdaRow { IsChecked = states.Length > 0 && states[0], Name = "", Value = "" },
                new UdaRow { IsChecked = states.Length > 1 && states[1], Name = "", Value = "" },
                new UdaRow { IsChecked = states.Length > 2 && states[2], Name = "", Value = "" },
                new UdaRow { IsChecked = states.Length > 3 && states[3], Name = "", Value = "" }
            };

            if (string.IsNullOrWhiteSpace(udaString)) return rows;

            var pairs = udaString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < pairs.Length && i < 4; i++)
            {
                var kvp = pairs[i].Split(new[] { '=' }, 2);
                if (kvp.Length == 2)
                {
                    rows[i].Name = kvp[0].Trim();
                    rows[i].Value = kvp[1].Trim();
                }
            }

            return rows;
        }

        /// <summary>
        /// Собирает отмеченные и заполненные строки UI обратно в системный формат Tekla.
        /// </summary>
        public static string Build(IEnumerable<UdaRow> rows)
        {
            var validRows = rows
                .Where(r => !string.IsNullOrWhiteSpace(r.Name))
                .Select(r => $"{r.Name.Trim()}={r.Value?.Trim() ?? ""}");

            return string.Join(";", validRows);
        }
    }
}