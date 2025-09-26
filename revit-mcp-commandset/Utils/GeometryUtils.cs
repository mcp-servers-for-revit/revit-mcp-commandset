using RevitMCPCommandSet.Models.Geometry;

namespace RevitMCPCommandSet.Utils
{
    public static class GeometryUtils
    {
        #region ## JZ Geometry和Revit互转

        /// <summary>
        /// 转换为Revit的XYZ点
        /// 单位转换：mm -> ft
        /// </summary>
        public static XYZ ToXYZ(this JZPoint jzPoint, double z = -1)
        {
            if (jzPoint == null) return null;
            if (z == -1)
            {
                return new XYZ(jzPoint.X / 304.8, jzPoint.Y / 304.8, jzPoint.Z / 304.8);
            }
            else
            {
                return new XYZ(jzPoint.X / 304.8, jzPoint.Y / 304.8, z);
            }
        }

        /// <summary>
        /// 从Revit的XYZ点创建
        /// </summary>
        public static JZPoint ToJZPoint(this XYZ xyz)
        {
            if (xyz == null) return null;
            return new JZPoint
            {
                X = xyz.X * 304.8,
                Y = xyz.Y * 304.8,
                Z = xyz.Z * 304.8
            };
        }

        /// <summary>
        /// 转换为Revit的Line
        /// 单位转换：mm -> ft
        /// </summary>
        public static Line ToLine(this JZLine jzLine, double z = -1)
        {
            if (jzLine.P0 == null || jzLine.P1 == null) return null;
            if (z == -1)
            {
                return Line.CreateBound(jzLine.P0.ToXYZ(), jzLine.P1.ToXYZ());
            }
            else
            {
                return Line.CreateBound(jzLine.P0.ToXYZ(z), jzLine.P1.ToXYZ(z));
            }
        }

        /// <summary>
        /// 从Revit的Line创建JZLine
        /// 默认考虑从英尺到毫米的单位转换（*/304.8）
        /// </summary>
        /// <param name="revitLine">Revit Line</param>
        /// <returns>JZLine实例</returns>
        public static JZLine ToJZLine(this Line revitLine)
        {
            if (revitLine == null) return null;
            // 将英尺转换为毫米 (1 ft = 304.8 mm)
            double factor = 304.8;

            return new JZLine
            {
                P0 = new JZPoint(
                    revitLine.GetEndPoint(0).X * factor,
                    revitLine.GetEndPoint(0).Y * factor,
                    revitLine.GetEndPoint(0).Z * factor
                ),
                P1 = new JZPoint(
                    revitLine.GetEndPoint(1).X * factor,
                    revitLine.GetEndPoint(1).Y * factor,
                    revitLine.GetEndPoint(1).Z * factor
                )
            };
        }

        /// <summary>
        /// 从Revit的Curve创建JZLine
        /// 默认考虑从英尺到毫米的单位转换（*/304.8）
        /// </summary>
        /// <param name="revitCurve">Revit Curve</param>
        /// <returns>JZLine实例</returns>
        public static JZLine ToJZLine(this Curve revitCurve)
        {
            if (revitCurve == null) return null;

            // 获取Curve的起点和终点
            XYZ startPoint = revitCurve.GetEndPoint(0);
            XYZ endPoint = revitCurve.GetEndPoint(1);

            // 将英尺转换为毫米 (1 ft = 304.8 mm)
            double factor = 304.8;

            return new JZLine
            {
                P0 = new JZPoint(startPoint.X * factor, startPoint.Y * factor, startPoint.Z * factor),
                P1 = new JZPoint(endPoint.X * factor, endPoint.Y * factor, endPoint.Z * factor)
            };
        }

        /// <summary>
        /// 从Revit的Face对象转换为JZFace对象，进行单位转换（304.8）
        /// </summary>
        /// <param name="revitFace">Revit的Face对象</param>
        /// <returns>转换后的JZFace对象</returns>
        public static JZFace ToJZFace(Face revitFace)
        {
            if (revitFace == null) return null;

            // 创建JZFace对象
            JZFace face = new JZFace();

            // 假设面是PlanarFace类型
            PlanarFace planarFace = revitFace as PlanarFace;
            if (planarFace != null)
            {
                // 获取面上的边界
                List<JZLine> outerLoop = new List<JZLine>();
                foreach (Curve curve in planarFace.GetEdgesAsCurveLoops()[0]) // 外环
                {
                    outerLoop.Add(curve.ToJZLine()); // 将Revit的Curve转换为JZLine
                }

                face.OuterLoop = outerLoop;

                // 如果有内环，获取内环的边界
                if (planarFace.GetEdgesAsCurveLoops().Count > 1)
                {
                    List<JZLine> innerLoop = new List<JZLine>();
                    foreach (Curve curve in planarFace.GetEdgesAsCurveLoops()[1]) // 内环
                    {
                        innerLoop.Add(curve.ToJZLine()); // 将Revit的Curve转换为JZLine
                    }

                    face.InnerLoops.Add(innerLoop);
                }
            }

            return face;
        }

        #endregion

        #region ## 新增统一转换方法

        /// <summary>
        /// 从Revit Location转换为LocationInfo
        /// </summary>
        public static Models.Geometry.LocationInfo FromLocation(Location location, Features.ElementFilter.FieldBuilders.FieldContext context = null)
        {
            if (location == null)
            {
                context?.AddWarning("元素没有位置信息");
                return null;
            }

            var info = new Models.Geometry.LocationInfo();

            if (location is LocationPoint locPoint)
            {
                info.Point = FromLocationPoint(locPoint);
                if (info.Point == null)
                    context?.AddWarning("无法获取点位置信息");
            }
            else if (location is LocationCurve locCurve)
            {
                info.Line = FromLocationCurve(locCurve, context);
                if (info.Line == null)
                    context?.AddWarning("无法获取线位置信息");
            }
            else
            {
                context?.AddWarning($"不支持的位置类型: {location.GetType().Name}");
                return null;
            }

            return info;
        }

        /// <summary>
        /// LocationPoint转JZPoint
        /// </summary>
        public static JZPoint FromLocationPoint(LocationPoint locationPoint)
        {
            var point = locationPoint?.Point;
            if (point == null) return null;
            return new JZPoint(point.X * 304.8, point.Y * 304.8, point.Z * 304.8);
        }

        /// <summary>
        /// LocationCurve转JZLine（处理非直线）
        /// </summary>
        public static JZLine FromLocationCurve(LocationCurve locationCurve, Features.ElementFilter.FieldBuilders.FieldContext context = null)
        {
            var curve = locationCurve?.Curve;
            if (curve == null) return null;

            // 获取起点和终点
            var start = curve.GetEndPoint(0);
            var end = curve.GetEndPoint(1);

            // 如果不是直线，添加警告
            if (!(curve is Line))
            {
                context?.AddWarning($"定位曲线类型 {curve.GetType().Name} 已简化为直线段");
            }

            return new JZLine(
                start.X * 304.8, start.Y * 304.8, start.Z * 304.8,
                end.X * 304.8, end.Y * 304.8, end.Z * 304.8
            );
        }

        /// <summary>
        /// BoundingBoxXYZ转BoundingBoxInfo
        /// </summary>
        public static Models.Geometry.BoundingBoxInfo FromBoundingBoxXYZ(BoundingBoxXYZ bbox)
        {
            if (bbox == null) return null;
            return new Models.Geometry.BoundingBoxInfo
            {
                Min = new JZPoint(bbox.Min.X * 304.8, bbox.Min.Y * 304.8, bbox.Min.Z * 304.8),
                Max = new JZPoint(bbox.Max.X * 304.8, bbox.Max.Y * 304.8, bbox.Max.Z * 304.8)
            };
        }

        /// <summary>
        /// CurveLoop转JZLine列表（处理非线性边）
        /// </summary>
        public static List<JZLine> FromCurveLoop(CurveLoop curveLoop, Features.ElementFilter.FieldBuilders.FieldContext context = null)
        {
            var lines = new List<JZLine>();
            bool hasNonLinearCurve = false;

            foreach (Curve curve in curveLoop)
            {
                if (!(curve is Line))
                {
                    hasNonLinearCurve = true;
                }

                var start = curve.GetEndPoint(0);
                var end = curve.GetEndPoint(1);
                lines.Add(new JZLine(
                    start.X * 304.8, start.Y * 304.8, start.Z * 304.8,
                    end.X * 304.8, end.Y * 304.8, end.Z * 304.8
                ));
            }

            if (hasNonLinearCurve)
            {
                context?.AddWarning("轮廓包含非直线边（如圆弧），已简化为直线段");
            }

            return lines;
        }

        #endregion
    }
}