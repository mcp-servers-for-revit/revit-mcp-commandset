using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitMCPCommandSet.Test
{
    /// <summary>
    /// 镜像元素技术验证命令
    /// 功能：提示用户选择一个元素，然后沿YZ平面镜像元素
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ValidateMirrorCommand : IExternalCommand
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
                    "请选择要镜像的元素（支持门、墙、楼板、灯具等）");

                if (reference == null)
                {
                    return Result.Cancelled;
                }

                ElementId elementId = reference.ElementId;
                Element selectedElement = doc.GetElement(elementId);

                // 2. 检查元素是否被锁定
                if (selectedElement.Pinned)
                {
                    TaskDialog.Show("错误", "选中的元素已被锁定，无法镜像。");
                    return Result.Failed;
                }

                // 3. 获取元素基本信息（用于显示）
                string elementInfo = GetElementInfo(selectedElement);

                // 4. 获取元素位置作为镜像平面的原点
                XYZ mirrorOrigin = GetElementLocation(selectedElement);
                if (mirrorOrigin == null)
                {
                    TaskDialog.Show("错误", "无法获取元素位置，无法执行镜像操作。");
                    return Result.Failed;
                }

                // 5. 创建镜像平面：通过元素位置的YZ平面（法向量为X轴方向）
                Plane mirrorPlane = Plane.CreateByNormalAndOrigin(XYZ.BasisX, mirrorOrigin);

                // 6. 在事务中执行镜像操作
                using (Transaction trans = new Transaction(doc, "镜像元素测试"))
                {
                    trans.Start();

                    try
                    {
                        // 使用ElementTransformUtils.MirrorElement进行镜像
                        ElementTransformUtils.MirrorElement(doc, elementId, mirrorPlane);

                        trans.Commit();

                        // 7. 显示成功消息
                        string successMessage = string.Format(
                            "镜像成功！\n\n" +
                            "元素信息：{0}\n" +
                            "镜像平面：YZ平面\n" +
                            "平面原点：({1:F2}, {2:F2}, {3:F2}) mm\n" +
                            "平面法向量：(1, 0, 0)\n" +
                            "镜像方向：沿X轴镜像",
                            elementInfo,
                            mirrorOrigin.X * 304.8,
                            mirrorOrigin.Y * 304.8,
                            mirrorOrigin.Z * 304.8);

                        TaskDialog.Show("镜像完成", successMessage);

                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        throw new Exception("镜像操作失败：" + ex.Message);
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
                // 使用线的中点作为镜像平面原点
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