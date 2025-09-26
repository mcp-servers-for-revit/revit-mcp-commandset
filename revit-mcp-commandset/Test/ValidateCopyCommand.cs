using System;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitMCPCommandSet.Test
{
    /// <summary>
    /// 复制元素技术验证命令
    /// 功能：提示用户选择一个元素，然后复制并向X正方向移动2000mm
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ValidateCopyCommand : IExternalCommand
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
                    "请选择要复制的元素（支持门、墙、楼板、灯具等）");

                if (reference == null)
                {
                    return Result.Cancelled;
                }

                ElementId elementId = reference.ElementId;
                Element selectedElement = doc.GetElement(elementId);

                // 2. 获取元素基本信息（用于显示）
                string elementInfo = GetElementInfo(selectedElement);

                // 3. 创建移动向量：X正方向2000mm
                // 单位转换：2000mm = 2000 / 304.8 英尺（Revit内部使用英尺）
                double distanceInFeet = 2000.0 / 304.8;
                XYZ copyTranslation = new XYZ(distanceInFeet, 0, 0);

                // 4. 在事务中执行复制操作
                using (Transaction trans = new Transaction(doc, "复制元素测试"))
                {
                    trans.Start();

                    try
                    {
                        // 使用ElementTransformUtils.CopyElement进行复制
                        ICollection<ElementId> copiedElementIds = ElementTransformUtils.CopyElement(
                            doc, elementId, copyTranslation);

                        trans.Commit();

                        // 5. 获取复制后的元素信息
                        string copiedElementsInfo = "";
                        int copiedCount = 0;
                        foreach (ElementId copiedId in copiedElementIds)
                        {
                            Element copiedElement = doc.GetElement(copiedId);
                            if (copiedElement != null)
                            {
                                copiedCount++;
                                copiedElementsInfo += string.Format("\n复制元素{0}: {1} (ID: {2})",
                                    copiedCount,
                                    copiedElement.Name,
                                    copiedId.IntegerValue);
                            }
                        }

                        // 6. 显示成功消息
                        string successMessage = string.Format(
                            "复制成功！\n\n" +
                            "原始元素信息：{0}\n" +
                            "复制数量：{1} 个元素\n" +
                            "复制偏移：X方向 2000mm\n" +
                            "移动向量：({2:F4}, 0, 0) 英尺" +
                            "{3}",
                            elementInfo,
                            copiedCount,
                            distanceInFeet,
                            copiedElementsInfo);

                        TaskDialog.Show("复制完成", successMessage);

                        // 7. 可选：选中复制后的元素便于查看
                        if (copiedElementIds.Count > 0)
                        {
                            uidoc.Selection.SetElementIds(copiedElementIds);
                        }

                        return Result.Succeeded;
                    }
                    catch (Exception ex)
                    {
                        trans.RollBack();
                        throw new Exception("复制操作失败：" + ex.Message);
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
    }
}