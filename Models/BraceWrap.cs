using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class BraceWrap
    {
        public Beam Beam { get; set; }
        public string Class { get; set; }

        public double h { get; set; }
        public double e1 { get; set; }
        public double e2 { get; set; }

        public Vector BraceDir { get; set; }
        public double ZAngle { get; set; }

        public Point CutOrigin { get; set; }
        public Point TopWeldPt { get; set; }
        public Point BotWeldPt { get; set; }

        public bool IsTop { get; set; }
        public bool IsBottom { get; set; }
        public bool IsStrut { get; set; }

        // НОВЫЙ ФЛАГ: Стыковой раскос
        public bool IsSplice { get; set; }
    }
}