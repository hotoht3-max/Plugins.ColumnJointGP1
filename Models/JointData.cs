using System.Collections.Generic;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class BraceSettings
    {
        public string Class { get; set; }
        public double h { get; set; }
        public double e1 { get; set; }
        public double e2 { get; set; }
        public int WeldType { get; set; }
        public double WeldSize { get; set; }
    }

    public class JointData
    {
        public double Offset_Web { get; set; }
        public int Offset_Web_Mode { get; set; }
        public double Offset_Gusset { get; set; }
        public double Offset_Brace { get; set; }

        public int Gusset_Shape_Mode { get; set; }
        public double? GussetRounding { get; set; }
        public int GP_PlanPos { get; set; }

        public int HoundEnabled { get; set; }
        public double HoundDistance { get; set; }
        public string Angle_Top { get; set; }
        public double Straight_Top { get; set; }
        public string Angle_Bot { get; set; }
        public double Straight_Bot { get; set; }

        public List<BraceSettings> BraceTypes { get; set; }

        public string Class_Exclude { get; set; }
        public string Class_Splice { get; set; }

        public int SpliceConnType { get; set; }
        public double Splice_h { get; set; }
        public double Splice_e1 { get; set; }
        public double Splice_e2 { get; set; }
        public int Splice_WType { get; set; }
        public double Splice_WSize { get; set; }

        public int Gusset_WType { get; set; }
        public double Gusset_WSize { get; set; }

        public PartSettings GussetPlate { get; set; }

        public double SpliceBolt_Size { get; set; }
        public string SpliceBolt_Standard { get; set; }
        public double SpliceBolt_Tol { get; set; }
        public int SpliceBolt_W1 { get; set; }
        public int SpliceBolt_W2 { get; set; }
        public int SpliceBolt_W3 { get; set; }
        public int SpliceBolt_N1 { get; set; }
        public int SpliceBolt_N2 { get; set; }
        public int SpliceBolt_Bolt { get; set; }

        public double SpliceBolt_Edge1 { get; set; }
        public string SpliceBolt_DistX { get; set; }
        public double SpliceBolt_Edge2 { get; set; }
        public string SpliceBolt_DistY { get; set; }
        public double SpliceBolt_Offset { get; set; }
    }
}