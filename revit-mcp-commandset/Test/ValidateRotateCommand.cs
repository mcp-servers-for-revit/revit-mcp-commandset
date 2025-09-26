using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitMCPCommandSet.Test
{
    /// <summary>
    /// 旋转元素技术验证命令
    /// 功能：提示用户选择一个元素，然后将其绕Z轴旋转90度
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ValidateRotateCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // 获取文档和UI文档
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1. 提示用户选择一个元素
                Reference reference = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    "请选择要旋转的元素（支持门、墙、楼板、灯具等）");

                if (reference == null)
                {
                    return Result.Cancelled;
                }

                ElementId elementId = reference.ElementId;
                Element selectedElement = doc.GetElement(elementId);

                // 2. 检查元素是否被锁定
                if (selectedElement.Pinned)
                {
                    TaskDialog.Show("错误", "选中的元素已被锁定，无法旋转。");
                    return Result.Failed;
                }

                // 3. 获取元素基本信息（用于显示）
                string elementInfo = GetElementInfo(selectedElement);

                // 4. 获取元素位置作为旋转轴的起点
                XYZ rotationOrigin = GetElementLocation(selectedElement);
                if (rotationOrigin == null)
                {
                    TaskDialog.Show("错误", "无法获取元素位置，无法执行旋转操作。");
                    return Result.Failed;
                }

                // 5. 创建旋转轴：从元素位置沿Z轴向上
                Line rotationAxis = Line.CreateBound(
                    rotationOrigin,
                    new XYZ(rotationOrigin.X, rotationOrigin.Y, rotationOrigin.Z + 10));

                // 6. 旋转角度：90度 = π/2 弧度
                double rotationAngle = Math.PI / 2.0;

                // 7. 在事务中执行旋转操作
                using (Transaction trans = new Transaction(doc, "旋转元素测试"))
                {
                    trans.Start();

                    try
                    {
                        // 使用ElementTransformUtils.RotateElement进行旋转
                        ElementTransformUtils.RotateElement(doc, elementId, rotationAxis, rotationAngle);

                        trans.Commit();

                        // 8. 显示成功消息
                        string successMessage = string.Format(
                            "旋转成功！\n\n" +
                            "元素信息：{0}\n" +
                            "旋转轴心：({1:F2}, {2:F2}, {3:F2}) mm\n" +
                            "旋转角度：90度（π/2 弧度）\n" +
                            "旋转方向：绕Z轴逆时针",
                            elementInfo,
                            rotationOrigin.X * 304.8,
                            rotationOrigin.Y * 304.8,
                            rotationOrigin.Z * 304.8);

                        TaskDialog.Show("旋转完成", successMessage);

                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        throw new Exception("旋转操作失败：" + ex.Message);
                    }
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // 用户取消选择
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("错误", "操作失败：" + ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>
        /// 获取元素的基本信息用于显示
        /// </summary>
        private string GetElementInfo(Element element)
        {
            string category = element.Category != null ? element.Category.Name : "未知类别";
            string elementType = element.GetType().Name;
            string name = element.Name;
            string id = element.Id.IntegerValue.ToString();

            // 获取位置类型信息
            string locationType = "未知";
            Location location = element.Location;
            if (location is LocationPoint)
            {
                locationType = "点定位";
                LocationPoint locPoint = location as LocationPoint;
                XYZ point = locPoint.Point;
                locationType += string.Format(" ({0:F2}, {1:F2}, {2:F2})",
                    point.X * 304.8, point.Y * 304.8, point.Z * 304.8); // 转换为毫米显示
            }
            else if (location is LocationCurve)
            {
                locationType = "线定位";
            }

            // 特殊类型识别
            string specialType = "";
            if (element is FamilyInstance)
            {
                FamilyInstance fi = element as FamilyInstance;
                specialType = "\n族类型：" + fi.Symbol.FamilyName;

                // 检查是否基于面
                if (fi.HostFace != null)
                {
                    specialType += " (基于面)";
                }
            }
            else if (element is Wall)
            {
                specialType = "\n系统族：墙";
            }
            else if (element is Floor)
            {
                specialType = "\n系统族：楼板";
            }

            return string.Format(
                "类别：{0}\n" +
                "名称：{1}\n" +
                "ID：{2}\n" +
                "位置类型：{3}" +
                "{4}",
                category, name, id, locationType, specialType);
        }

        /// <summary>
        /// 获取元素的位置坐标
        /// </summary>
        private XYZ GetElementLocation(Element element)
        {
            Location location = element.Location;

            if (location is LocationPoint)
            {
                LocationPoint locPoint = location as LocationPoint;
                return locPoint.Point;
            }
            else if (location is LocationCurve)
            {
                LocationCurve locCurve = location as LocationCurve;
                Curve curve = locCurve.Curve;
                // 使用线的中点作为旋转中心
                return curve.Evaluate(0.5, true);
            }
            else
            {
                // 尝试使用包围盒中心
                BoundingBoxXYZ bbox = element.get_BoundingBox(null);
                if (bbox != null)
                {
                    return (bbox.Min + bbox.Max) * 0.5;
                }
            }

            return null;
        }
    }
}