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
            Logger.Write("Вход в геометрическое ядро BuildNode (Исправленные Умные Дефолты)");

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

                // 5. ГЕНЕРАЦИЯ ПОЛИГОНА ФАСОНКИ
                List<Point> finalPolygon = new List<Point>();
                var topB = braces.First();
                var botB = braces.Last();

                // --- Верхний угол ---
                double topW_x = new Vector(topB.TopWeldPt.X - pCenter.X, topB.TopWeldPt.Y - pCenter.Y, topB.TopWeldPt.Z - pCenter.Z).Dot(v_X);
                double topW_y = new Vector(topB.TopWeldPt.X - pCenter.X, topB.TopWeldPt.Y - pCenter.Y, topB.TopWeldPt.Z - pCenter.Z).Dot(v_Z);

                double rx_top, ry_top;

                if (string.IsNullOrWhiteSpace(data.Angle_Top))
                {
                    // ИСПРАВЛЕНИЕ: Раскос смотрит ВВЕРХ (ZAngle > 0) -> Горизонтальный рез
                    if (topB.IsStrut || topB.ZAngle > 1e-4)
                    {
                        rx_top = -1; ry_top = 0;
                    }
                    else
                    {
                        double cx = -topB.BraceDir.Dot(v_X);
                        double cy = -topB.BraceDir.Dot(v_Z);
                        double newAngle = Math.Atan2(cy, cx);
                        rx_top = Math.Cos(newAngle); ry_top = Math.Sin(newAngle);
                    }
                }
                else
                {
                    double.TryParse(data.Angle_Top, out double angleDeg);
                    double cx = -topB.BraceDir.Dot(v_X);
                    double cy = -topB.BraceDir.Dot(v_Z);
                    double newAngle = Math.Atan2(cy, cx) - (angleDeg * Math.PI / 180.0);
                    rx_top = Math.Cos(newAngle); ry_top = Math.Sin(newAngle);
                }

                if (Math.Abs(rx_top) < 1e-4) rx_top = -1e-4;

                double limitX_top = gussetStartX + data.Straight_Top;
                double cornerY_top = topW_y + (ry_top / rx_top) * (limitX_top - topW_x);

                Point pCornerTop = new Point(pCenter);
                pCornerTop.Translate(v_X.X * limitX_top + v_Z.X * cornerY_top, v_X.Y * limitX_top + v_Z.Y * cornerY_top, v_X.Z * limitX_top + v_Z.Z * cornerY_top);

                Point pColTop = new Point(pCenter);
                pColTop.Translate(v_X.X * gussetStartX + v_Z.X * cornerY_top, v_X.Y * gussetStartX + v_Z.Y * cornerY_top, v_X.Z * gussetStartX + v_Z.Z * cornerY_top);

                finalPolygon.Add(pColTop);
                if (Math.Abs(data.Straight_Top) > 1e-3) finalPolygon.Add(pCornerTop);

                // --- Швы раскосов ---
                foreach (var b in braces)
                {
                    finalPolygon.Add(b.TopWeldPt);
                    finalPolygon.Add(b.BotWeldPt);
                }

                // --- Нижний угол ---
                double botW_x = new Vector(botB.BotWeldPt.X - pCenter.X, botB.BotWeldPt.Y - pCenter.Y, botB.BotWeldPt.Z - pCenter.Z).Dot(v_X);
                double botW_y = new Vector(botB.BotWeldPt.X - pCenter.X, botB.BotWeldPt.Y - pCenter.Y, botB.BotWeldPt.Z - pCenter.Z).Dot(v_Z);

                double rx_bot, ry_bot;

                if (string.IsNullOrWhiteSpace(data.Angle_Bot))
                {
                    // ИСПРАВЛЕНИЕ: Раскос смотрит ВНИЗ (ZAngle < 0) -> Горизонтальный рез
                    if (botB.IsStrut || botB.ZAngle < -1e-4)
                    {
                        rx_bot = -1; ry_bot = 0;
                    }
                    else
                    {
                        double cx = -botB.BraceDir.Dot(v_X);
                        double cy = -botB.BraceDir.Dot(v_Z);
                        double newAngle = Math.Atan2(cy, cx);
                        rx_bot = Math.Cos(newAngle); ry_bot = Math.Sin(newAngle);
                    }
                }
                else
                {
                    double.TryParse(data.Angle_Bot, out double angleDeg);
                    double cx = -botB.BraceDir.Dot(v_X);
                    double cy = -botB.BraceDir.Dot(v_Z);
                    double newAngle = Math.Atan2(cy, cx) + (angleDeg * Math.PI / 180.0);
                    rx_bot = Math.Cos(newAngle); ry_bot = Math.Sin(newAngle);
                }

                if (Math.Abs(rx_bot) < 1e-4) rx_bot = -1e-4;

                double limitX_bot = gussetStartX + data.Straight_Bot;
                double cornerY_bot = botW_y + (ry_bot / rx_bot) * (limitX_bot - botW_x);

                Point pCornerBot = new Point(pCenter);
                pCornerBot.Translate(v_X.X * limitX_bot + v_Z.X * cornerY_bot, v_X.Y * limitX_bot + v_Z.Y * cornerY_bot, v_X.Z * limitX_bot + v_Z.Z * cornerY_bot);

                Point pColBot = new Point(pCenter);
                pColBot.Translate(v_X.X * gussetStartX + v_Z.X * cornerY_bot, v_X.Y * gussetStartX + v_Z.Y * cornerY_bot, v_X.Z * gussetStartX + v_Z.Z * cornerY_bot);

                if (Math.Abs(data.Straight_Bot) > 1e-3) finalPolygon.Add(pCornerBot);
                finalPolygon.Add(pColBot);

                // --- РЕНДЕР ФАСОНКИ ---
                CreateRoughGusset(finalPolygon, data);
                Logger.Write("Фасонка успешно построена.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сбой внутри BuildNode: {ex.Message}", ex);
            }
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