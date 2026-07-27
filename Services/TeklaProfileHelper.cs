using System;
using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class TeklaProfileHelper
    {
        public static void GetActualDimensions(Part part, out double height, out double width, out double webThickness)
        {
            height = 0.0;
            width = 0.0;
            webThickness = 0.0;

            part.GetReportProperty("PROFILE.HEIGHT", ref height);
            part.GetReportProperty("PROFILE.WIDTH", ref width);
            part.GetReportProperty("PROFILE.WEB_THICKNESS", ref webThickness);

            // Резервный вариант, если это не двутавр/швеллер
            if (webThickness <= 0) webThickness = height;
        }
    }
}