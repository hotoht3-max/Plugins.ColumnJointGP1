using RAM.Plugins.ColumnJointGP1.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tekla.Structures.Geometry3d;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;

namespace RAM.Plugins.ColumnJointGP1.Services
{
    public static class NodeGeometryBuilder
    {
        public static void BuildNode(Part branch, List<Part> lacings, JointData data)
        {
            Logger.Write("Вход в геометрическое ядро BuildNode (Итерация 3: Right-Side Convex Hull)");

            if (lacings.Count == 0 || !(branch is Beam branchBeam)) return;

            try
            {
                // 1. НАХОДИМ ТЕОРЕТИЧЕСКИЙ ЦЕНТР УЗЛА
                Line branchLine = new Line(branchBeam.StartPoint, branchBeam.EndPoint);
                double sumX = 0, sumY = 0, sumZ = 0;
                int count = 0;

                foreach (var lacing in lacings)
                {
                    if (lacing is Beam beam)
                    {
                        LineSegment shortestSeg = Intersection.LineToLine(branchLine, new Line(beam.StartPoint, beam.EndPoint));
                        if (shortestSeg != null)
                        {
                            Point mid = new Point(
                                (shortestSeg.Point1.X + shortestSeg.Point2.X) / 2.0,
                                (shortestSeg.Point1.Y + shortestSeg.Point2.Y) / 2.0,
                                (shortestSeg.Point1.Z + shortestSeg.Point2.Z) / 2.0);
                            Point proj = Projection.PointToLine(mid, branchLine);
                            sumX += proj.X; sumY += proj.Y; sumZ += proj.Z;
                            count++;
                        }
                    }
                }

                if (count == 0) return;
                Point pCenter = new Point(sumX / count, sumY / count, sumZ / count);

                // 2. СТРОИМ ЛОКАЛЬНУЮ СИСТЕМУ КООРДИНАТ ФЕРМЫ
                Vector v_Z = new Vector(branchBeam.EndPoint.X - branchBeam.StartPoint.X,
                                        branchBeam.EndPoint.Y - branchBeam.StartPoint.Y,
                                        branchBeam.EndPoint.Z - branchBeam.StartPoint.Z).GetNormal();

                Vector v_Y = new Vector();
                foreach (var lacing in lacings)
                {
                    if (lacing is Beam b)
                    {
                        Vector lDir = new Vector(b.EndPoint.X - b.StartPoint.X, b.EndPoint.Y - b.StartPoint.Y, b.EndPoint.Z - b.StartPoint.Z).GetNormal();
                        v_Y = v_Z.Cross(lDir).GetNormal();
                        if (v_Y.GetLength() > 0.1) break;
                    }
                }

                Vector v_X = v_Y.Cross(v_Z).GetNormal();

                var braces = JointManager.ProcessBraces(lacings, pCenter, v_Z, data);
                if (braces.Count == 0) return;

                if (braces[0].BraceDir.Dot(v_X) < 0)
                {
                    v_X *= -1.0;
                    v_Y *= -1.0;
                }

                // 3. АНАЛИЗ ФИЗИЧЕСКИХ ГРАНИЦ КОЛОННЫ
                double maxBranchX = GetMaxProjection(branchBeam.GetSolid(), pCenter, v_X);
                TeklaProfileHelper.GetActualDimensions(branchBeam, out _, out _, out double branchTw);

                double limitWeb = (data.Offset_Web_Mode == 0)
                    ? ((branchTw / 2.0) + data.Offset_Web)
                    : (maxBranchX + data.Offset_Web);

                double gussetStartX = maxBranchX + data.Offset_Gusset;

                double limitStrutUp = data.Offset_Brace / 2.0;
                double limitStrutDown = data.Offset_Brace / 2.0;

                var strutWrap = braces.FirstOrDefault(b => b.IsStrut);
                if (strutWrap != null)
                {
                    limitStrutUp = GetMaxProjection(strutWrap.Beam.GetSolid(), pCenter, v_Z) + data.Offset_Brace;
                    limitStrutDown = GetMaxProjection(strutWrap.Beam.GetSolid(), pCenter, v_Z * -1.0) + data.Offset_Brace;
                }

                // 4. РАСЧЕТ ПОДРЕЗОК И ТОЧЕК ШВОВ
                foreach (var b in braces)
                {
                    double rWeb = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, v_X * -1.0);
                    double u_x = b.BraceDir.Dot(v_X);
                    double t_web = (u_x > 1e-4) ? (limitWeb + rWeb) / u_x : 0;
                    double t_final = t_web;

                    if (!b.IsStrut)
                    {
                        double u_z = b.BraceDir.Dot(v_Z);
                        double t_strut = 0;
                        if (u_z > 1e-4)
                        {
                            double rStrut = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, v_Z * -1.0);
                            t_strut = (limitStrutUp + rStrut) / u_z;
                        }
                        else if (u_z < -1e-4)
                        {
                            double rStrut = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, v_Z);
                            t_strut = (limitStrutDown + rStrut) / Math.Abs(u_z);
                        }
                        t_final = Math.Max(t_web, t_strut);
                    }

                    t_final = Math.Max(0, t_final);
                    b.CutOrigin = new Point(pCenter);
                    b.CutOrigin.Translate(b.BraceDir.X * t_final, b.BraceDir.Y * t_final, b.BraceDir.Z * t_final);

                    CreateFitting(b.Beam, b.CutOrigin, b.BraceDir, v_Y);

                    Vector transDir = v_Y.Cross(b.BraceDir).GetNormal();
                    double rTrans1 = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, transDir);
                    double rTrans2 = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, transDir * -1.0);

                    Point pCut1 = new Point(b.CutOrigin);
                    pCut1.Translate(transDir.X * (rTrans1 + b.e1), transDir.Y * (rTrans1 + b.e1), transDir.Z * (rTrans1 + b.e1));

                    Point pCut2 = new Point(b.CutOrigin);
                    pCut2.Translate(transDir.X * -(rTrans2 + b.e2), transDir.Y * -(rTrans2 + b.e2), transDir.Z * -(rTrans2 + b.e2));

                    Point pWeld1 = new Point(pCut1);
                    pWeld1.Translate(b.BraceDir.X * b.h, b.BraceDir.Y * b.h, b.BraceDir.Z * b.h);

                    Point pWeld2 = new Point(pCut2);
                    pWeld2.Translate(b.BraceDir.X * b.h, b.BraceDir.Y * b.h, b.BraceDir.Z * b.h);

                    if (transDir.Dot(v_Z) > 0)
                    {
                        b.TopWeldPt = pWeld1; b.BotWeldPt = pWeld2;
                    }
                    else
                    {
                        b.TopWeldPt = pWeld2; b.BotWeldPt = pWeld1;
                    }
                }

                Func<Point, double> GetX = pt => new Vector(pt.X - pCenter.X, pt.Y - pCenter.Y, pt.Z - pCenter.Z).Dot(v_X);
                Func<Point, double> GetZ = pt => new Vector(pt.X - pCenter.X, pt.Y - pCenter.Y, pt.Z - pCenter.Z).Dot(v_Z);
                Func<double, double, Point> ToGlobal = (x, z) => {
                    Point p = new Point(pCenter);
                    p.Translate(v_X.X * x + v_Z.X * z, v_X.Y * x + v_Z.Y * z, v_X.Z * x + v_Z.Z * z);
                    return p;
                };

                // 5. ГЕНЕРАЦИЯ ПОЛИГОНА ФАСОНКИ (STRATEGY ROUTER)
                List<Point> finalPolygon = new List<Point>();
                var topB = braces.First();
                var botB = braces.Last();

                bool isRectangular = (data.Gusset_Shape_Mode == 0);

                if (isRectangular)
                {
                    // --- СТРАТЕГИЯ А: ПРЯМОУГОЛЬНИК ---
                    double maxZ = braces.Max(b => Math.Max(GetZ(b.TopWeldPt), GetZ(b.BotWeldPt))) + data.Straight_Top;
                    double minZ = braces.Min(b => Math.Min(GetZ(b.TopWeldPt), GetZ(b.BotWeldPt))) - data.Straight_Bot;
                    double maxX = braces.Max(b => Math.Max(GetX(b.TopWeldPt), GetX(b.BotWeldPt)));

                    finalPolygon.Add(ToGlobal(gussetStartX, maxZ));
                    finalPolygon.Add(ToGlobal(maxX, maxZ));
                    finalPolygon.Add(ToGlobal(maxX, minZ));
                    finalPolygon.Add(ToGlobal(gussetStartX, minZ));
                }
                else
                {
                    // --- СТРАТЕГИЯ В: ФИГУРНАЯ ---

                    // --- ВЕРХНИЙ КРАЙ ---
                    if (topB.IsSplice)
                    {
                        finalPolygon.Add(ToGlobal(gussetStartX, GetZ(topB.TopWeldPt)));
                    }
                    else
                    {
                        CalculateCorner(pCenter, v_X, v_Z, topB, data.Angle_Top, data.Straight_Top, gussetStartX, true, out Point pCornerTop, out Point pColTop);
                        finalPolygon.Add(pColTop);
                        if (Math.Abs(data.Straight_Top) > 1e-3) finalPolygon.Add(pCornerTop);
                    }

                    // --- ЦЕНТР (Стыки раскосов) ---
                    bool hasStrut = braces.Any(b => b.IsStrut);

                    if (braces.Count == 2 && hasStrut && data.Two_Brace_Mode == 0)
                    {
                        // Ручная стратегия ступенчатой черновой фасонки для 2х раскосов
                        var b0 = braces[0]; var b1 = braces[1];
                        finalPolygon.Add(b0.TopWeldPt);

                        double x0 = GetX(b0.BotWeldPt); double z0 = GetZ(b0.BotWeldPt);
                        double x1 = GetX(b1.TopWeldPt); double z1 = GetZ(b1.TopWeldPt);
                        double maxX = Math.Max(x0, x1);

                        finalPolygon.Add(b0.BotWeldPt);
                        finalPolygon.Add(ToGlobal(maxX, z0));
                        finalPolygon.Add(ToGlobal(maxX, z1));
                        finalPolygon.Add(b1.TopWeldPt);

                        finalPolygon.Add(b1.BotWeldPt);
                    }
                    else
                    {
                        // Алгоритм выпуклой оболочки правого контура (Right-Side Convex Hull)
                        // Он автоматически создает идеальные фаски для выпирающих раскосов и 
                        // прямые скосы над утопленными, гарантированно покрывая заданный h.
                        var pts2d = new List<Point>();
                        foreach (var b in braces)
                        {
                            pts2d.Add(b.TopWeldPt);
                            pts2d.Add(b.BotWeldPt);
                        }

                        // Сортировка: сверху вниз, затем слева направо
                        pts2d = pts2d.OrderByDescending(p => GetZ(p)).ThenByDescending(p => GetX(p)).ToList();

                        var hull = new List<Point>();
                        foreach (var p in pts2d)
                        {
                            while (hull.Count >= 2)
                            {
                                var p1 = hull[hull.Count - 2];
                                var p2 = hull[hull.Count - 1];
                                var p3 = p;

                                // Векторное произведение (Cross Product) для определения поворота
                                double cross = (GetX(p2) - GetX(p1)) * (GetZ(p3) - GetZ(p2)) - (GetZ(p2) - GetZ(p1)) * (GetX(p3) - GetX(p2));

                                // Если поворот левый или точки на одной прямой - удаляем "внутреннюю" точку шва
                                if (cross >= -1e-5) hull.RemoveAt(hull.Count - 1);
                                else break;
                            }
                            hull.Add(p);
                        }

                        finalPolygon.AddRange(hull);
                    }

                    // --- НИЖНИЙ КРАЙ ---
                    if (botB.IsSplice)
                    {
                        finalPolygon.Add(ToGlobal(gussetStartX, GetZ(botB.BotWeldPt)));
                    }
                    else
                    {
                        CalculateCorner(pCenter, v_X, v_Z, botB, data.Angle_Bot, data.Straight_Bot, gussetStartX, false, out Point pCornerBot, out Point pColBot);
                        if (Math.Abs(data.Straight_Bot) > 1e-3) finalPolygon.Add(pCornerBot);
                        finalPolygon.Add(pColBot);
                    }
                }

                // --- РЕНДЕР ФАСОНКИ ---
                CreateRoughGusset(finalPolygon, data);
                Logger.Write("Успешное применение стратегии.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сбой внутри BuildNode: {ex.Message}", ex);
            }
        }

        private static void CalculateCorner(Point pCenter, Vector v_X, Vector v_Z, BraceWrap brace, string angleStr, double straightLen, double gussetStartX, bool isTop, out Point cornerPt, out Point colPt)
        {
            Point weldPt = isTop ? brace.TopWeldPt : brace.BotWeldPt;
            double w_x = new Vector(weldPt.X - pCenter.X, weldPt.Y - pCenter.Y, weldPt.Z - pCenter.Z).Dot(v_X);
            double w_y = new Vector(weldPt.X - pCenter.X, weldPt.Y - pCenter.Y, weldPt.Z - pCenter.Z).Dot(v_Z);

            double rx, ry;

            if (string.IsNullOrWhiteSpace(angleStr))
            {
                bool isHorizontalDefault = isTop ? (brace.IsStrut || brace.ZAngle > 1e-4) : (brace.IsStrut || brace.ZAngle < -1e-4);
                if (isHorizontalDefault)
                {
                    rx = -1; ry = 0;
                }
                else
                {
                    double cx = -brace.BraceDir.Dot(v_X);
                    double cy = -brace.BraceDir.Dot(v_Z);
                    double newAngle = Math.Atan2(cy, cx);
                    rx = Math.Cos(newAngle); ry = Math.Sin(newAngle);
                }
            }
            else
            {
                double.TryParse(angleStr, out double angleDeg);
                double cx = -brace.BraceDir.Dot(v_X);
                double cy = -brace.BraceDir.Dot(v_Z);
                double sign = isTop ? -1.0 : 1.0;
                double newAngle = Math.Atan2(cy, cx) + (sign * angleDeg * Math.PI / 180.0);
                rx = Math.Cos(newAngle); ry = Math.Sin(newAngle);
            }

            if (Math.Abs(rx) < 1e-4) rx = -1e-4;

            double limitX = gussetStartX + straightLen;
            double cornerY = w_y + (ry / rx) * (limitX - w_x);

            cornerPt = new Point(pCenter);
            cornerPt.Translate(v_X.X * limitX + v_Z.X * cornerY, v_X.Y * limitX + v_Z.Y * cornerY, v_X.Z * limitX + v_Z.Z * cornerY);

            colPt = new Point(pCenter);
            colPt.Translate(v_X.X * gussetStartX + v_Z.X * cornerY, v_X.Y * gussetStartX + v_Z.Y * cornerY, v_X.Z * gussetStartX + v_Z.Z * cornerY);
        }

        private static double GetProfileRadius(Beam beam, Vector direction)
        {
            TeklaProfileHelper.GetActualDimensions(beam, out double h, out double w, out _);
            CoordinateSystem sys = beam.GetCoordinateSystem();
            return (Math.Abs(direction.Dot(sys.AxisY.GetNormal())) * h + Math.Abs(direction.Dot(sys.AxisX.Cross(sys.AxisY).GetNormal())) * w) / 2.0;
        }

        private static double GetMaxProjection(Solid solid, Point origin, Vector direction)
        {
            double maxProj = 0;
            if (solid == null) return maxProj;

            EdgeEnumerator edgeEnum = solid.GetEdgeEnumerator();
            while (edgeEnum.MoveNext())
            {
                if (edgeEnum.Current is Edge edge)
                {
                    double proj1 = new Vector(edge.StartPoint.X - origin.X, edge.StartPoint.Y - origin.Y, edge.StartPoint.Z - origin.Z).Dot(direction);
                    double proj2 = new Vector(edge.EndPoint.X - origin.X, edge.EndPoint.Y - origin.Y, edge.EndPoint.Z - origin.Z).Dot(direction);
                    maxProj = Math.Max(maxProj, Math.Max(proj1, proj2));
                }
            }
            return maxProj;
        }

        private static double GetMaxTransverseProjection(Solid solid, Point origin, Vector axialDir, Vector projDir)
        {
            double maxProj = 0;
            if (solid == null) return maxProj;

            EdgeEnumerator edgeEnum = solid.GetEdgeEnumerator();
            while (edgeEnum.MoveNext())
            {
                if (edgeEnum.Current is Edge edge)
                {
                    Vector v1 = new Vector(edge.StartPoint.X - origin.X, edge.StartPoint.Y - origin.Y, edge.StartPoint.Z - origin.Z);
                    Vector r1 = new Vector(v1.X - axialDir.X * v1.Dot(axialDir), v1.Y - axialDir.Y * v1.Dot(axialDir), v1.Z - axialDir.Z * v1.Dot(axialDir));

                    Vector v2 = new Vector(edge.EndPoint.X - origin.X, edge.EndPoint.Y - origin.Y, edge.EndPoint.Z - origin.Z);
                    Vector r2 = new Vector(v2.X - axialDir.X * v2.Dot(axialDir), v2.Y - axialDir.Y * v2.Dot(axialDir), v2.Z - axialDir.Z * v2.Dot(axialDir));

                    maxProj = Math.Max(maxProj, Math.Max(r1.Dot(projDir), r2.Dot(projDir)));
                }
            }
            return maxProj;
        }

        private static void CreateFitting(Beam targetPart, Point planeOrigin, Vector braceDir, Vector planeNormalY)
        {
            Vector axisX = planeNormalY.Cross(braceDir).GetNormal();
            Vector axisY = planeNormalY;

            Fitting fitting = new Fitting
            {
                Plane = new Plane { Origin = planeOrigin, AxisX = axisX, AxisY = axisY },
                Father = targetPart
            };
            fitting.Insert();
        }

        private static void CreateRoughGusset(List<Point> polygonPoints, JointData data)
        {
            if (polygonPoints.Count < 3) return;

            ContourPlate gusset = new ContourPlate();

            foreach (var pt in polygonPoints)
            {
                gusset.AddContourPoint(new ContourPoint(pt, null));
            }

            gusset.Profile.ProfileString = data.GussetPlate.Profile;
            gusset.Material.MaterialString = data.GussetPlate.Material;
            gusset.Class = data.GussetPlate.Class;
            gusset.Name = data.GussetPlate.Name;

            if (!string.IsNullOrEmpty(data.GussetPlate.PartPrefix)) gusset.PartNumber.Prefix = data.GussetPlate.PartPrefix;
            gusset.PartNumber.StartNumber = data.GussetPlate.PartStartNo;

            gusset.Position.Depth = Position.DepthEnum.MIDDLE;
            gusset.Insert();
        }
    }
}