using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Models
{
    public class BraceWrap
    {
        public Beam Beam { get; set; }
        public string Class { get; set; }

        // Индивидуальные параметры шва
        public double h { get; set; }
        public double e1 { get; set; }
        public double e2 { get; set; }

        // Геометрические векторы
        public Vector BraceDir { get; set; } // Вектор всегда ОТ центра узла
        public double ZAngle { get; set; }   // Косинус угла к оси колонны (для сортировки)

        // Ключевые точки
        public Point CutOrigin { get; set; }
        public Point TopWeldPt { get; set; }
        public Point BotWeldPt { get; set; }

        // Роли раскоса
        public bool IsTop { get; set; }
        public bool IsBottom { get; set; }
        public bool IsStrut { get; set; }
    }
}