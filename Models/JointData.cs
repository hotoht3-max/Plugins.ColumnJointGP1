using System.Collections.Generic;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    // Обертка для хранения настроек конкретного класса раскоса
    public class BraceSettings
    {
        public string Class { get; set; }
        public double h { get; set; }
        public double e1 { get; set; }
        public double e2 { get; set; }
    }

    public class JointData
    {
        // Глобальные отступы
        public double Offset_Web { get; set; }
        public int Offset_Web_Mode { get; set; }
        public double Offset_Gusset { get; set; }
        public double Offset_Brace { get; set; }

        // Настройки углов и прямых участков фасонки
        public string Angle_Top { get; set; }
        public double Straight_Top { get; set; }
        public string Angle_Bot { get; set; }
        public double Straight_Bot { get; set; }

        // Таблица типов раскосов
        public List<BraceSettings> BraceTypes { get; set; }

        // Классы-исключения и стыки (через пробел)
        public string Class_Exclude { get; set; }
        public string Class_Splice { get; set; }

        // Атрибуты детали
        public PartSettings GussetPlate { get; set; }
    }
}