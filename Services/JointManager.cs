using RAM.Plugins.ColumnJointGP1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class JointManager
    {
        public static List<BraceWrap> ProcessBraces(List<Part> lacings, Point pCenter, Vector v_Z, JointData data)
        {
            var result = new List<BraceWrap>();

            var excludeList = string.IsNullOrWhiteSpace(data.Class_Exclude)
                ? new string[0]
                : data.Class_Exclude.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in lacings)
            {
                if (!(part is Beam beam)) continue;
                if (excludeList.Contains(beam.Class)) continue;

                double h = 150.0, e1 = 30.0, e2 = 30.0;

                var settings = data.BraceTypes?.LastOrDefault(s => s.Class == beam.Class);
                if (settings != null)
                {
                    h = settings.h;
                    e1 = settings.e1;
                    e2 = settings.e2;
                }

                double d1 = Distance(beam.StartPoint, pCenter);
                double d2 = Distance(beam.EndPoint, pCenter);
                Vector braceDir = (d1 < d2)
                    ? new Vector(beam.EndPoint.X - beam.StartPoint.X, beam.EndPoint.Y - beam.StartPoint.Y, beam.EndPoint.Z - beam.StartPoint.Z).GetNormal()
                    : new Vector(beam.StartPoint.X - beam.EndPoint.X, beam.StartPoint.Y - beam.EndPoint.Y, beam.StartPoint.Z - beam.EndPoint.Z).GetNormal();

                result.Add(new BraceWrap
                {
                    Beam = beam,
                    Class = beam.Class,
                    h = h,
                    e1 = e1,
                    e2 = e2,
                    BraceDir = braceDir,
                    ZAngle = braceDir.Dot(v_Z)
                });
            }

            result = result.OrderByDescending(b => b.ZAngle).ToList();

            if (result.Count > 0)
            {
                result.First().IsTop = true;
                result.Last().IsBottom = true;
            }

            foreach (var b in result)
            {
                if (Math.Abs(b.ZAngle) < 0.25) b.IsStrut = true;
            }

            return result;
        }

        private static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
        }
    }
}