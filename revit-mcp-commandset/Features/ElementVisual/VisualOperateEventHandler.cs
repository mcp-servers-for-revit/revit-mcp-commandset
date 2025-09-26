using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementVisual.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Features.ElementVisual
{
    public class VisualOperateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        /// <summary>
        /// 事件等待对象
        /// </summary>
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        /// <summary>
        /// 操作数据（传入数据）
        /// </summary>
        public VisualOperationSetting OperationData { get; private set; }

        /// <summary>
        /// 执行结果（传出数据）
        /// </summary>
        public AIResult<ElementOperationResponse> Result { get; private set; }

        /// <summary>
        /// 设置操作参数
        /// </summary>
        public void SetParameters(VisualOperationSetting data)
        {
            OperationData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                // 参数验证
                OperationData.Validate();

                // 执行视觉操作
                var result = ExecuteVisualOperation(uiDoc, OperationData);

                Result = result;
            }
            catch (Exception ex)
            {
                Result = new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"视觉操作失败: {ex.Message}",
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = OperationData?.ElementIds?.Count ?? 0,
                        SuccessfulElements = new List<int>(),
                        FailedElements = OperationData?.ElementIds?.Select(id => new FailureInfo
                        {
                            ElementId = id,
                            Reason = ex.Message
                        }).ToList() ?? new List<FailureInfo>()
                    }
                };
            }
            finally
            {
                _resetEvent.Set(); // 通知等待线程操作已完成
            }
        }

        /// <summary>
        /// 等待操作完成
        /// </summary>
        /// <param name="timeoutMilliseconds">超时时间（毫秒）</param>
        /// <returns>操作是否在超时前完成</returns>
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        /// <summary>
        /// IExternalEventHandler.GetName 实现
        /// </summary>
        public string GetName()
        {
            return "Visual操作元素";
        }

        /// <summary>
        /// 执行视觉操作（复用现有逻辑）
        /// </summary>
        private AIResult<ElementOperationResponse> ExecuteVisualOperation(
            UIDocument uidoc,
            VisualOperationSetting setting)
        {
            var successfulElements = new List<int>();
            var failedElements = new List<FailureInfo>();
            var details = new Dictionary<string, object>();

            // 将int类型的元素ID转换为ElementId类型
            ICollection<ElementId> elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();

            try
            {
                // 根据操作类型执行不同的操作（复用 OperateElementEventHandler 逻辑）
                switch (setting.VisualAction)
                {
                    case "Select":
                        // 选择元素
                        uidoc.Selection.SetElementIds(elementIds);
                        successfulElements.AddRange(setting.ElementIds);
                        break;

                    case "SelectionBox":
                        // 创建3D剖切框（复用现有逻辑）
                        var boxResult = CreateSelectionBox(uidoc, elementIds);
                        if (boxResult.Success)
                        {
                            successfulElements.AddRange(setting.ElementIds);
                            details["viewSwitched"] = boxResult.ViewSwitched;
                            details["targetViewName"] = boxResult.TargetViewName;
                        }
                        else
                        {
                            throw new Exception(boxResult.ErrorMessage);
                        }
                        break;

                    case "Highlight":
                        // 高亮（设置为红色）
                        using (Transaction trans = new Transaction(doc, "高亮元素"))
                        {
                            trans.Start();
                            SetElementsColor(doc, elementIds, new int[] { 255, 0, 0 });
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        details["appliedColor"] = new int[] { 255, 0, 0 };
                        uidoc.ShowElements(elementIds);
                        break;

                    case "SetColor":
                        // 设置自定义颜色
                        using (Transaction trans = new Transaction(doc, "设置元素颜色"))
                        {
                            trans.Start();
                            SetElementsColor(doc, elementIds, setting.ColorValue);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        details["appliedColor"] = setting.ColorValue;
                        uidoc.ShowElements(elementIds);
                        break;

                    case "SetTransparency":
                        // 设置透明度
                        using (Transaction trans = new Transaction(doc, "设置元素透明度"))
                        {
                            trans.Start();
                            SetElementsTransparency(doc, elementIds, setting.TransparencyValue.Value);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        details["appliedTransparency"] = setting.TransparencyValue.Value;
                        break;

                    default:
                        throw new Exception($"未支持的操作类型：{setting.VisualAction}");
                }

                return new AIResult<ElementOperationResponse>
                {
                    Success = true,
                    Message = $"成功对 {successfulElements.Count} 个元素执行 {setting.VisualAction} 操作",
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = setting.ElementIds.Count,
                        SuccessfulElements = successfulElements,
                        FailedElements = failedElements,
                        Details = details
                    }
                };
            }
            catch (Exception ex)
            {
                return new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"视觉操作失败: {ex.Message}",
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = setting.ElementIds.Count,
                        SuccessfulElements = successfulElements,
                        FailedElements = setting.ElementIds.Select(id => new FailureInfo
                        {
                            ElementId = id,
                            Reason = ex.Message
                        }).ToList(),
                        Details = details
                    }
                };
            }
        }

        /// <summary>
        /// 创建3D剖切框（复用现有逻辑）
        /// </summary>
        private (bool Success, bool ViewSwitched, string TargetViewName, string ErrorMessage) CreateSelectionBox(
            UIDocument uidoc,
            ICollection<ElementId> elementIds)
        {
            try
            {
                Document doc = uidoc.Document;
                View3D targetView;
                bool viewSwitched = false;

                if (doc.ActiveView is View3D)
                {
                    targetView = doc.ActiveView as View3D;
                }
                else
                {
                    FilteredElementCollector collector = new FilteredElementCollector(doc);
                    collector.OfClass(typeof(View3D));

                    targetView = collector
                        .Cast<View3D>()
                        .FirstOrDefault(v =>
                            !v.IsTemplate && !v.IsLocked &&
                            (v.Name.Contains("{3D}") || v.Name.Contains("Default 3D")));

                    if (targetView == null)
                    {
                        return (false, false, null, "无法找到合适的3D视图用于创建剖切框");
                    }

                    uidoc.ActiveView = targetView;
                    viewSwitched = true;
                }

                // 计算包围盒（复用现有逻辑）
                BoundingBoxXYZ boundingBox = CalculateElementsBoundingBox(doc, elementIds);
                if (boundingBox == null)
                {
                    return (false, viewSwitched, targetView.Name, "无法为所选元素创建边界框");
                }

                // 设置剖切框
                using (Transaction trans = new Transaction(doc, "创建剖切框"))
                {
                    trans.Start();
                    targetView.IsSectionBoxActive = true;
                    targetView.SetSectionBox(boundingBox);
                    trans.Commit();
                }

                uidoc.ShowElements(elementIds);
                return (true, viewSwitched, targetView.Name, null);
            }
            catch (Exception ex)
            {
                return (false, false, null, ex.Message);
            }
        }

        /// <summary>
        /// 计算元素的包围盒
        /// </summary>
        private BoundingBoxXYZ CalculateElementsBoundingBox(Document doc, ICollection<ElementId> elementIds)
        {
            BoundingBoxXYZ boundingBox = null;

            foreach (ElementId id in elementIds)
            {
                Element elem = doc.GetElement(id);
                if (elem == null) continue;

                BoundingBoxXYZ elemBox = elem.get_BoundingBox(null);
                if (elemBox != null)
                {
                    if (boundingBox == null)
                    {
                        boundingBox = new BoundingBoxXYZ
                        {
                            Min = new XYZ(elemBox.Min.X, elemBox.Min.Y, elemBox.Min.Z),
                            Max = new XYZ(elemBox.Max.X, elemBox.Max.Y, elemBox.Max.Z)
                        };
                    }
                    else
                    {
                        boundingBox.Min = new XYZ(
                            Math.Min(boundingBox.Min.X, elemBox.Min.X),
                            Math.Min(boundingBox.Min.Y, elemBox.Min.Y),
                            Math.Min(boundingBox.Min.Z, elemBox.Min.Z));

                        boundingBox.Max = new XYZ(
                            Math.Max(boundingBox.Max.X, elemBox.Max.X),
                            Math.Max(boundingBox.Max.Y, elemBox.Max.Y),
                            Math.Max(boundingBox.Max.Z, elemBox.Max.Z));
                    }
                }
            }

            if (boundingBox != null)
            {
                // 增加边界框尺寸
                double offset = 1.0; // 1英尺的偏移
                boundingBox.Min = new XYZ(boundingBox.Min.X - offset, boundingBox.Min.Y - offset,
                    boundingBox.Min.Z - offset);
                boundingBox.Max = new XYZ(boundingBox.Max.X + offset, boundingBox.Max.Y + offset,
                    boundingBox.Max.Z + offset);
            }

            return boundingBox;
        }

        /// <summary>
        /// 设置元素透明度
        /// </summary>
        private void SetElementsTransparency(Document doc, ICollection<ElementId> elementIds, int transparencyValue)
        {
            // 确保值在0-100范围内
            int clampedValue = Math.Max(0, Math.Min(100, transparencyValue));

            // 创建图形覆盖设置对象
            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
            overrideSettings.SetSurfaceTransparency(clampedValue);

            // 对每个元素应用透明度设置
            foreach (ElementId id in elementIds)
            {
                doc.ActiveView.SetElementOverrides(id, overrideSettings);
            }
        }

        /// <summary>
        /// 在视图中将指定的元素设置为指定颜色
        /// </summary>
        /// <param name="doc">文档</param>
        /// <param name="elementIds">要设置颜色的元素ID集合</param>
        /// <param name="elementColor">颜色值（RGB格式）</param>
        private static void SetElementsColor(Document doc, ICollection<ElementId> elementIds, int[] elementColor)
        {
            // 检查颜色数组是否有效
            if (elementColor == null || elementColor.Length < 3)
            {
                elementColor = new int[] { 255, 0, 0 }; // 默认红色
            }

            // 确保RGB值在0-255范围内
            int r = Math.Max(0, Math.Min(255, elementColor[0]));
            int g = Math.Max(0, Math.Min(255, elementColor[1]));
            int b = Math.Max(0, Math.Min(255, elementColor[2]));

            // 创建Revit颜色对象
            Color color = new Color((byte)r, (byte)g, (byte)b);

            // 创建图形覆盖设置
            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
            overrideSettings.SetProjectionLineColor(color);
            overrideSettings.SetCutLineColor(color);
            overrideSettings.SetSurfaceForegroundPatternColor(color);
            overrideSettings.SetSurfaceBackgroundPatternColor(color);

            // 设置填充图案
            try
            {
                FilteredElementCollector patternCollector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FillPatternElement));

                FillPatternElement solidPattern = patternCollector
                    .Cast<FillPatternElement>()
                    .FirstOrDefault(p => p.GetFillPattern().IsSolidFill);

                if (solidPattern != null)
                {
                    overrideSettings.SetSurfaceForegroundPatternId(solidPattern.Id);
                    overrideSettings.SetSurfaceForegroundPatternVisible(true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"设置填充图案失败: {ex.Message}");
            }

            // 对每个元素应用覆盖设置
            foreach (ElementId id in elementIds)
            {
                doc.ActiveView.SetElementOverrides(id, overrideSettings);
            }
        }
    }
}