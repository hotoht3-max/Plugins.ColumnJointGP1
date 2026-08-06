using System.Collections.Generic;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class BraceSettings
    {
        public string Class { get; set; }
        public double h { get; set; }
        public double e1 { get; set; }
        public double e2 { get; set; }
    }

    public class JointData
    {
        public double Offset_Web { get; set; }
        public int Offset_Web_Mode { get; set; }
        public double Offset_Gusset { get; set; }
        public double Offset_Brace { get; set; }

        // НОВЫЕ ПАРАМЕТРЫ ФОРМООБРАЗОВАНИЯ
        public int Gusset_Shape_Mode { get; set; } // 0 - Прямоугольная, 1 - Фигурная
        public double? GussetRounding { get; set; } // Nullable: если пусто - не округляем
        public int HoundEnabled { get; set; }
        public double HoundDistance { get; set; }
        public string Angle_Top { get; set; }
        public double Straight_Top { get; set; }
        public string Angle_Bot { get; set; }
        public double Straight_Bot { get; set; }

        public List<BraceSettings> BraceTypes { get; set; }

        public string Class_Exclude { get; set; }
        public string Class_Splice { get; set; }

        public PartSettings GussetPlate { get; set; }
    }
}