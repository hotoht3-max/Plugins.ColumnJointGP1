using RAM.Plugins.ColumnJointGP1.Models;
using System;
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
            Logger.Write("Вход в геометрическое ядро BuildNode (Пространственный радар v2)");

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

                var allBraces = JointManager.ProcessBraces(lacings, pCenter, v_Z, data);
                if (allBraces.Count == 0) return;

                if (allBraces[0].BraceDir.Dot(v_X) < 0)
                {
                    v_X *= -1.0;
                    v_Y *= -1.0;
                }

                // 3. АНАЛИЗ ФИЗИЧЕСКИХ ГРАНИЦ КОЛОННЫ ПО ОСЯМ X И Z
                double maxBranchX = GetMaxProjection(branchBeam.GetSolid(), pCenter, v_X);
                TeklaProfileHelper.GetActualDimensions(branchBeam, out _, out _, out double branchTw);

                double limitWeb = (data.Offset_Web_Mode == 0)
                    ? ((branchTw / 2.0) + data.Offset_Web)
                    : (maxBranchX + data.Offset_Web);

                double gussetStartX = maxBranchX + data.Offset_Gusset;

                double zColMin = double.MinValue;
                double zColMax = double.MaxValue;
                bool isHoundActive = data.HoundEnabled == 1;

                if (isHoundActive)
                {
                    Solid branchSolid = branchBeam.GetSolid();
                    if (branchSolid != null)
                    {
                        double currentMin = double.MaxValue;
                        double currentMax = double.MinValue;
                        EdgeEnumerator edgeEnum = branchSolid.GetEdgeEnumerator();
                        while (edgeEnum.MoveNext())
                        {
                            if (edgeEnum.Current is Edge edge)
                            {
                                double z1 = new Vector(edge.StartPoint.X - pCenter.X, edge.StartPoint.Y - pCenter.Y, edge.StartPoint.Z - pCenter.Z).Dot(v_Z);
                                double z2 = new Vector(edge.EndPoint.X - pCenter.X, edge.EndPoint.Y - pCenter.Y, edge.EndPoint.Z - pCenter.Z).Dot(v_Z);
                                if (z1 < currentMin) currentMin = z1;
                                if (z1 > currentMax) currentMax = z1;
                                if (z2 < currentMin) currentMin = z2;
                                if (z2 > currentMax) currentMax = z2;
                            }
                        }
                        zColMin = currentMin;
                        zColMax = currentMax;
                    }
                    else
                    {
                        isHoundActive = false;
                    }
                }

                // 4. ГРУППИРОВКА РАСКОСОВ
                var bracesA = allBraces.Where(b =>
                    new Vector(b.Beam.StartPoint.X - pCenter.X, b.Beam.StartPoint.Y - pCenter.Y, b.Beam.StartPoint.Z - pCenter.Z).Dot(v_Y) >= -10.0).ToList();
                var bracesB = allBraces.Where(b =>
                    new Vector(b.Beam.StartPoint.X - pCenter.X, b.Beam.StartPoint.Y - pCenter.Y, b.Beam.StartPoint.Z - pCenter.Z).Dot(v_Y) < -10.0).ToList();

                var groups = new List<List<BraceWrap>>();
                if (bracesA.Count > 0) groups.Add(bracesA);
                if (bracesB.Count > 0) groups.Add(bracesB);

                double t_pl = 10.0;
                if (!string.IsNullOrEmpty(data.GussetPlate.Profile))
                {
                    string pstr = data.GussetPlate.Profile.ToUpper().Replace("PL", "").Trim();
                    if (pstr.Contains("*")) pstr = pstr.Split('*')[0];
                    double.TryParse(pstr, out t_pl);
                }

                // ==========================================================
                // 5. ГЕНЕРАЦИЯ ДЛЯ КАЖДОЙ ПЛОСКОСТИ
                // ==========================================================
                foreach (var currentBraces in groups)
                {
                    double signY = (currentBraces == bracesA) ? 1.0 : -1.0;
                    Vector currentVY = v_Y * signY;

                    double colMaxY = GetMaxProjection(branchBeam.GetSolid(), pCenter, currentVY);
                    double y_center = 0;

                    if (data.GP_PlanPos == 1) // Снаружи (по колонне)
                    {
                        y_center = colMaxY + (t_pl / 2.0);
                    }
                    else // Заподлицо (по уголкам)
                    {
                        double closestFaceY = double.MaxValue;
                        bool attachOutsideFace = true;

                        foreach (var b in currentBraces)
                        {
                            double braceMin = double.MaxValue;
                            double braceMax = double.MinValue;
                            var solid = b.Beam.GetSolid();
                            if (solid != null)
                            {
                                EdgeEnumerator edgeEnum = solid.GetEdgeEnumerator();
                                while (edgeEnum.MoveNext())
                                {
                                    if (edgeEnum.Current is Edge edge)
                                    {
                                        double p1 = new Vector(edge.StartPoint.X - pCenter.X, edge.StartPoint.Y - pCenter.Y, edge.StartPoint.Z - pCenter.Z).Dot(currentVY);
                                        double p2 = new Vector(edge.EndPoint.X - pCenter.X, edge.EndPoint.Y - pCenter.Y, edge.EndPoint.Z - pCenter.Z).Dot(currentVY);
                                        if (p1 < braceMin) braceMin = p1;
                                        if (p1 > braceMax) braceMax = p1;
                                        if (p2 < braceMin) braceMin = p2;
                                        if (p2 > braceMax) braceMax = p2;
                                    }
                                }
                            }

                            // Правило: внутри колонны -> крепим к наружной грани. Снаружи -> ко внутренней.
                            bool isInside = (braceMax <= colMaxY + 2.0);
                            double candidateFace = isInside ? braceMax : braceMin;

                            // Ищем самый смещенный внутрь уголок (ближе к центру)
                            if (Math.Abs(candidateFace) < Math.Abs(closestFaceY))
                            {
                                closestFaceY = candidateFace;
                                attachOutsideFace = isInside;
                            }
                        }

                        if (closestFaceY != double.MaxValue)
                        {
                            y_center = attachOutsideFace ? (closestFaceY + t_pl / 2.0) : (closestFaceY - t_pl / 2.0);
                        }
                        else
                        {
                            y_center = colMaxY + (t_pl / 2.0); // Фолбэк
                        }
                    }

                    // Подрезки и точки швов для текущей группы
                    double limitStrutUp = data.Offset_Brace / 2.0;
                    double limitStrutDown = data.Offset_Brace / 2.0;

                    var strutWrap = currentBraces.FirstOrDefault(b => b.IsStrut);
                    if (strutWrap != null)
                    {
                        limitStrutUp = GetMaxProjection(strutWrap.Beam.GetSolid(), pCenter, v_Z) + data.Offset_Brace;
                        limitStrutDown = GetMaxProjection(strutWrap.Beam.GetSolid(), pCenter, v_Z * -1.0) + data.Offset_Brace;
                    }

                    foreach (var b in currentBraces)
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

                        CreateFitting(b.Beam, b.CutOrigin, b.BraceDir, currentVY);

                        Vector transDir = currentVY.Cross(b.BraceDir).GetNormal();
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

                    // Замыкание переменных для ToGlobal
                    Vector locVY = currentVY;
                    double locYCenter = y_center;

                    Func<Point, double> GetX = pt => new Vector(pt.X - pCenter.X, pt.Y - pCenter.Y, pt.Z - pCenter.Z).Dot(v_X);
                    Func<Point, double> GetZ = pt => new Vector(pt.X - pCenter.X, pt.Y - pCenter.Y, pt.Z - pCenter.Z).Dot(v_Z);
                    Func<double, double, Point> ToGlobal = (x, z) => {
                        Point p = new Point(pCenter);
                        p.Translate(v_X.X * x + locVY.X * locYCenter + v_Z.X * z,
                                    v_X.Y * x + locVY.Y * locYCenter + v_Z.Y * z,
                                    v_X.Z * x + locVY.Z * locYCenter + v_Z.Z * z);
                        return p;
                    };

                    Func<Point, Vector, List<Point>, double> GetHFact = (rayOrigin, rayDir, poly) => {
                        double Ox = GetX(rayOrigin), Oz = GetZ(rayOrigin);
                        double Dx = rayDir.Dot(v_X), Dz = rayDir.Dot(v_Z);
                        double maxT = double.MinValue;
                        bool found = false;

                        for (int j = 0; j < poly.Count; j++)
                        {
                            Point p1 = poly[j];
                            Point p2 = poly[(j + 1) % poly.Count];

                            double P1x = GetX(p1), P1z = GetZ(p1);
                            double P2x = GetX(p2), P2z = GetZ(p2);

                            double Vx = P2x - P1x;
                            double Vz = P2z - P1z;

                            double det = Vx * Dz - Vz * Dx;
                            if (Math.Abs(det) < 1e-5) continue;

                            double dX = P1x - Ox;
                            double dZ = P1z - Oz;

                            double t = (dZ * Vx - dX * Vz) / det;
                            double s = (dZ * Dx - dX * Dz) / det;

                            if (s >= -1e-3 && s <= 1.001)
                            {
                                if (!found || t > maxT)
                                {
                                    maxT = t;
                                    found = true;
                                }
                            }
                        }
                        return found ? maxT : double.NaN;
                    };

                    // Итеративный солвер
                    List<Point> finalPolygon = new List<Point>();
                    double[] shiftTop = new double[currentBraces.Count];
                    double[] shiftBot = new double[currentBraces.Count];

                    for (int iter = 0; iter < 10; iter++)
                    {
                        finalPolygon.Clear();
                        var currentTopWeld = new List<Point>();
                        var currentBotWeld = new List<Point>();

                        for (int i = 0; i < currentBraces.Count; i++)
                        {
                            var b = currentBraces[i];
                            Point cT = new Point(b.TopWeldPt);
                            cT.Translate(b.BraceDir.X * shiftTop[i], b.BraceDir.Y * shiftTop[i], b.BraceDir.Z * shiftTop[i]);
                            currentTopWeld.Add(cT);

                            Point cB = new Point(b.BotWeldPt);
                            cB.Translate(b.BraceDir.X * shiftBot[i], b.BraceDir.Y * shiftBot[i], b.BraceDir.Z * shiftBot[i]);
                            currentBotWeld.Add(cB);
                        }

                        var topB = currentBraces.First();
                        var botB = currentBraces.Last();

                        bool isRectangular = (data.Gusset_Shape_Mode == 0);
                        if (currentBraces.Any(b => b.IsSplice))
                        {
                            isRectangular = false;
                        }

                        if (isRectangular)
                        {
                            double maxZ = currentBraces.Select((b, i) => Math.Max(GetZ(currentTopWeld[i]), GetZ(currentBotWeld[i]))).Max() + data.Straight_Top;
                            double minZ = currentBraces.Select((b, i) => Math.Min(GetZ(currentTopWeld[i]), GetZ(currentBotWeld[i]))).Min() - data.Straight_Bot;
                            double maxX = currentBraces.Select((b, i) => Math.Max(GetX(currentTopWeld[i]), GetX(currentBotWeld[i]))).Max();

                            if (isHoundActive)
                            {
                                if (zColMax > 0 && zColMax <= data.HoundDistance) maxZ = zColMax;
                                if (zColMin < 0 && Math.Abs(zColMin) <= data.HoundDistance) minZ = zColMin;
                            }

                            if (iter == 9 && data.GussetRounding.HasValue && data.GussetRounding.Value > 0)
                            {
                                double step = data.GussetRounding.Value;
                                double width = maxX - gussetStartX;
                                double height = maxZ - minZ;
                                double newWidth = Math.Ceiling(width / step) * step;
                                double newHeight = Math.Ceiling(height / step) * step;
                                maxX = gussetStartX + newWidth;
                                double deltaZ = newHeight - height;
                                maxZ += deltaZ / 2.0;
                                minZ -= deltaZ / 2.0;
                            }

                            finalPolygon.Add(ToGlobal(gussetStartX, maxZ));
                            finalPolygon.Add(ToGlobal(maxX, maxZ));
                            finalPolygon.Add(ToGlobal(maxX, minZ));
                            finalPolygon.Add(ToGlobal(gussetStartX, minZ));
                        }
                        else
                        {
                            bool topIsHound = isHoundActive && zColMax > 0 && zColMax <= data.HoundDistance;
                            Point pColTop, pCornerTop;

                            if (topIsHound)
                            {
                                pColTop = ToGlobal(gussetStartX, zColMax);
                                pCornerTop = ToGlobal(gussetStartX + data.Straight_Top, zColMax);
                            }
                            else if (topB.IsSplice)
                            {
                                pColTop = ToGlobal(gussetStartX, GetZ(currentTopWeld[0]));
                                pCornerTop = pColTop;
                            }
                            else
                            {
                                CalculateCorner(pCenter, v_X, v_Z, ToGlobal, topB, data.Angle_Top, data.Straight_Top, gussetStartX, true, currentTopWeld[0], out pCornerTop, out pColTop);
                            }

                            bool botIsHound = isHoundActive && zColMin < 0 && Math.Abs(zColMin) <= data.HoundDistance;
                            Point pColBot, pCornerBot;

                            if (botIsHound)
                            {
                                pColBot = ToGlobal(gussetStartX, zColMin);
                                pCornerBot = ToGlobal(gussetStartX + data.Straight_Bot, zColMin);
                            }
                            else if (botB.IsSplice)
                            {
                                pColBot = ToGlobal(gussetStartX, GetZ(currentBotWeld.Last()));
                                pCornerBot = pColBot;
                            }
                            else
                            {
                                CalculateCorner(pCenter, v_X, v_Z, ToGlobal, botB, data.Angle_Bot, data.Straight_Bot, gussetStartX, false, currentBotWeld.Last(), out pCornerBot, out pColBot);
                            }

                            var pts2d = new List<Point>();
                            for (int i = 0; i < currentBraces.Count; i++)
                            {
                                pts2d.Add(currentTopWeld[i]);
                                pts2d.Add(currentBotWeld[i]);
                            }

                            if (topIsHound) pts2d.Add(pCornerTop);
                            if (botIsHound) pts2d.Add(pCornerBot);

                            pts2d = pts2d.OrderByDescending(p => GetZ(p)).ThenByDescending(p => GetX(p)).ToList();

                            var hull = new List<Point>();
                            foreach (var p in pts2d)
                            {
                                while (hull.Count >= 2)
                                {
                                    var p1 = hull[hull.Count - 2];
                                    var p2 = hull[hull.Count - 1];
                                    var p3 = p;
                                    double cross = (GetX(p2) - GetX(p1)) * (GetZ(p3) - GetZ(p2)) - (GetZ(p2) - GetZ(p1)) * (GetX(p3) - GetX(p2));
                                    if (cross >= -1e-5) hull.RemoveAt(hull.Count - 1);
                                    else break;
                                }
                                hull.Add(p);
                            }

                            finalPolygon.Add(pColTop);
                            if (!topIsHound && Math.Abs(data.Straight_Top) > 1e-3) finalPolygon.Add(pCornerTop);
                            finalPolygon.AddRange(hull.Select(pt => ToGlobal(GetX(pt), GetZ(pt))));
                            if (!botIsHound && Math.Abs(data.Straight_Bot) > 1e-3) finalPolygon.Add(pCornerBot);
                            finalPolygon.Add(pColBot);
                        }

                        if (iter == 9) break;

                        for (int i = 0; i < currentBraces.Count; i++)
                        {
                            var b = currentBraces[i];
                            Vector transDir = currentVY.Cross(b.BraceDir).GetNormal();
                            double rTrans1 = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, transDir);
                            double rTrans2 = GetMaxTransverseProjection(b.Beam.GetSolid(), pCenter, b.BraceDir, transDir * -1.0);

                            Point oTopEdge, oBotEdge;
                            if (transDir.Dot(v_Z) > 0)
                            {
                                oTopEdge = new Point(b.CutOrigin); oTopEdge.Translate(transDir.X * rTrans1, transDir.Y * rTrans1, transDir.Z * rTrans1);
                                oBotEdge = new Point(b.CutOrigin); oBotEdge.Translate(transDir.X * -rTrans2, transDir.Y * -rTrans2, transDir.Z * -rTrans2);
                            }
                            else
                            {
                                oTopEdge = new Point(b.CutOrigin); oTopEdge.Translate(transDir.X * -rTrans2, transDir.Y * -rTrans2, transDir.Z * -rTrans2);
                                oBotEdge = new Point(b.CutOrigin); oBotEdge.Translate(transDir.X * rTrans1, transDir.Y * rTrans1, transDir.Z * rTrans1);
                            }

                            double hFactTop = GetHFact(oTopEdge, b.BraceDir, finalPolygon);
                            double hFactBot = GetHFact(oBotEdge, b.BraceDir, finalPolygon);

                            if (!double.IsNaN(hFactTop))
                            {
                                double diff = b.h - hFactTop;
                                if (diff > 100) diff = 100; if (diff < -100) diff = -100;
                                shiftTop[i] += diff * 0.75;
                            }
                            if (!double.IsNaN(hFactBot))
                            {
                                double diff = b.h - hFactBot;
                                if (diff > 100) diff = 100; if (diff < -100) diff = -100;
                                shiftBot[i] += diff * 0.75;
                            }
                        }
                    }

                    CreateRoughGusset(finalPolygon, data);
                }

                // --- ТЕСТОВЫЙ БОЛТ ---
                BoltArray testBolt = new BoltArray();
                testBolt.PartToBeBolted = branchBeam;
                testBolt.PartToBoltTo = branchBeam;

                // Располагаем болт в теоретическом центре узла
                testBolt.FirstPosition = pCenter;
                testBolt.SecondPosition = new Point(pCenter.X, pCenter.Y, pCenter.Z + 100);

                // Применяем настройки из UI
                testBolt.BoltSize = data.SpliceBolt_Size;
                testBolt.BoltStandard = data.SpliceBolt_Standard;
                testBolt.Tolerance = data.SpliceBolt_Tol;

                // Применяем маску комплекта
                testBolt.Washer1 = data.SpliceBolt_W1 == 1;
                testBolt.Washer2 = data.SpliceBolt_W2 == 1;
                testBolt.Washer3 = data.SpliceBolt_W3 == 1;
                testBolt.Nut1 = data.SpliceBolt_N1 == 1;
                testBolt.Nut2 = data.SpliceBolt_N2 == 1;
                testBolt.Bolt = data.SpliceBolt_Bolt == 1;

                testBolt.Position.Depth = Position.DepthEnum.MIDDLE;
                testBolt.Position.Plane = Position.PlaneEnum.MIDDLE;
                testBolt.Position.Rotation = Position.RotationEnum.FRONT;

                // Создаем группу из 1 болта (расстояния 0)
                testBolt.AddBoltDistX(0);
                testBolt.AddBoltDistY(0);

                testBolt.Insert();

                Logger.Write("Успешное применение пространственного радара.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Сбой внутри BuildNode: {ex.Message}", ex);
            }
        }
        // ... (остальные вспомогательные методы остаются без изменений)

        private static void CalculateCorner(Point pCenter, Vector v_X, Vector v_Z, Func<double, double, Point> toGlobal, BraceWrap brace, string angleStr, double straightLen, double gussetStartX, bool isTop, Point weldPt, out Point cornerPt, out Point colPt)
        {
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

            cornerPt = toGlobal(limitX, cornerY);
            colPt = toGlobal(gussetStartX, cornerY);
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