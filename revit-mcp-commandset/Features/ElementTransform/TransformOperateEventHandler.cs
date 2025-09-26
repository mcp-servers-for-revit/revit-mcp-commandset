using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Geometry;
using RevitMCPCommandSet.Features.ElementTransform.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Features.ElementTransform
{
    public class TransformOperateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public TransformOperationSetting OperationData { get; private set; }
        public AIResult<ElementOperationResponse> Result { get; private set; }

        public void SetParameters(TransformOperationSetting data)
        {
            OperationData = data;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                OperationData.Validate();
                var result = ExecuteTransformOperation(uiDoc, OperationData);
                Result = result;
            }
            catch (Exception ex)
            {
                Result = new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"变换操作失败: {ex.Message}",
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
                _resetEvent.Set();
            }
        }

        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public string GetName()
        {
            return "Transform操作元素";
        }

        private AIResult<ElementOperationResponse> ExecuteTransformOperation(
            UIDocument uidoc,
            TransformOperationSetting setting)
        {
            var successfulElements = new List<int>();
            var failedElements = new List<FailureInfo>();
            var details = new Dictionary<string, object>();

            ICollection<ElementId> elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();

            try
            {
                using (Transaction trans = new Transaction(doc, $"{setting.TransformAction}操作"))
                {
                    trans.Start();

                    switch (setting.TransformAction)
                    {
                        case "Rotate":
                            ExecuteRotate(elementIds, setting, successfulElements, failedElements, details);
                            break;

                        case "Mirror":
                            ExecuteMirror(elementIds, setting, successfulElements, failedElements, details);
                            break;

                        case "Flip":
                            ExecuteFlip(elementIds, setting, successfulElements, failedElements, details);
                            break;

                        case "Move":
                            ExecuteMove(elementIds, setting, successfulElements, failedElements, details);
                            break;

                        default:
                            throw new Exception($"未支持的操作类型：{setting.TransformAction}");
                    }

                    trans.Commit();
                }

                return new AIResult<ElementOperationResponse>
                {
                    Success = failedElements.Count == 0,
                    Message = failedElements.Count == 0
                        ? $"成功对 {successfulElements.Count} 个元素执行 {setting.TransformAction} 操作"
                        : $"{setting.TransformAction} 操作部分失败：成功 {successfulElements.Count} 个，失败 {failedElements.Count} 个",
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
                    Message = $"变换操作失败: {ex.Message}",
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
        /// 执行旋转操作
        /// </summary>
        private void ExecuteRotate(
            ICollection<ElementId> elementIds,
            TransformOperationSetting setting,
            List<int> successfulElements,
            List<FailureInfo> failedElements,
            Dictionary<string, object> details)
        {
            // 构建旋转轴
            Line axis = JZLine.ToLine(setting.RotateAxis);

            // 角度转弧度
            double angleRad = setting.RotateAngle * Math.PI / 180.0;

            foreach (var elemId in elementIds)
            {
                try
                {
                    ElementTransformUtils.RotateElement(doc, elemId, axis, angleRad);
                    successfulElements.Add(elemId.IntegerValue);
                }
                catch (Exception ex)
                {
                    failedElements.Add(new FailureInfo
                    {
                        ElementId = elemId.IntegerValue,
                        Reason = $"旋转失败: {ex.Message}"
                    });
                }
            }

            details["rotateAngle"] = setting.RotateAngle;
            details["rotateAxis"] = new {
                p0 = setting.RotateAxis.P0,
                p1 = setting.RotateAxis.P1
            };
        }

        /// <summary>
        /// 执行镜像操作
        /// </summary>
        private void ExecuteMirror(
            ICollection<ElementId> elementIds,
            TransformOperationSetting setting,
            List<int> successfulElements,
            List<FailureInfo> failedElements,
            Dictionary<string, object> details)
        {
            Plane plane = setting.MirrorPlane.ToRevitPlane();

            foreach (var elemId in elementIds)
            {
                try
                {
                    ElementTransformUtils.MirrorElement(doc, elemId, plane);
                    successfulElements.Add(elemId.IntegerValue);
                }
                catch (Exception ex)
                {
                    failedElements.Add(new FailureInfo
                    {
                        ElementId = elemId.IntegerValue,
                        Reason = $"镜像失败: {ex.Message}"
                    });
                }
            }

            details["mirrorPlane"] = new
            {
                origin = setting.MirrorPlane.Origin,
                normal = setting.MirrorPlane.Normal
            };
        }

        /// <summary>
        /// 执行翻转操作
        /// </summary>
        private void ExecuteFlip(
            ICollection<ElementId> elementIds,
            TransformOperationSetting setting,
            List<int> successfulElements,
            List<FailureInfo> failedElements,
            Dictionary<string, object> details)
        {
            foreach (var elemId in elementIds)
            {
                try
                {
                    Element elem = doc.GetElement(elemId);
                    if (elem is FamilyInstance familyInstance)
                    {
                        if (setting.FlipDirection == "Hand" && familyInstance.CanFlipHand)
                        {
                            // 使用事务中的Flip操作，等效于flipHand()
                            if (doc.GetElement(familyInstance.Id) is FamilyInstance fi)
                            {
                                fi.flipHand();
                            }
                            successfulElements.Add(elemId.IntegerValue);
                        }
                        else if (setting.FlipDirection == "Facing" && familyInstance.CanFlipFacing)
                        {
                            // 使用事务中的Flip操作，等效于flipFacing()
                            if (doc.GetElement(familyInstance.Id) is FamilyInstance fi)
                            {
                                fi.flipFacing();
                            }
                            successfulElements.Add(elemId.IntegerValue);
                        }
                        else
                        {
                            failedElements.Add(new FailureInfo
                            {
                                ElementId = elemId.IntegerValue,
                                Reason = $"元素不支持 {setting.FlipDirection} 翻转"
                            });
                        }
                    }
                    else
                    {
                        failedElements.Add(new FailureInfo
                        {
                            ElementId = elemId.IntegerValue,
                            Reason = "仅族实例支持翻转操作"
                        });
                    }
                }
                catch (Exception ex)
                {
                    failedElements.Add(new FailureInfo
                    {
                        ElementId = elemId.IntegerValue,
                        Reason = $"翻转失败: {ex.Message}"
                    });
                }
            }

            details["flipDirection"] = setting.FlipDirection;
        }

        /// <summary>
        /// 执行移动操作（P1阶段：directTransform策略）
        /// </summary>
        private void ExecuteMove(
            ICollection<ElementId> elementIds,
            TransformOperationSetting setting,
            List<int> successfulElements,
            List<FailureInfo> failedElements,
            Dictionary<string, object> details)
        {
            // 构建移动向量（毫米转英尺）
            XYZ translation = new XYZ(
                setting.MoveVector.X / 304.8,
                setting.MoveVector.Y / 304.8,
                setting.MoveVector.Z / 304.8
            );

            foreach (var elemId in elementIds)
            {
                try
                {
                    Element elem = doc.GetElement(elemId);

                    // 检查元素是否可移动
                    if (!CanMoveElement(elem))
                    {
                        failedElements.Add(new FailureInfo
                        {
                            ElementId = elemId.IntegerValue,
                            Reason = "元素类型不支持directTransform移动（建议使用recreate策略，P2实现）"
                        });
                        continue;
                    }

                    ElementTransformUtils.MoveElement(doc, elemId, translation);
                    successfulElements.Add(elemId.IntegerValue);
                }
                catch (Exception ex)
                {
                    failedElements.Add(new FailureInfo
                    {
                        ElementId = elemId.IntegerValue,
                        Reason = $"移动失败: {ex.Message}（可能需要recreate策略，P2实现）"
                    });
                }
            }

            details["moveVector"] = setting.MoveVector;
            details["moveStrategy"] = "directTransform";
            details["strategyNote"] = "P1阶段仅支持directTransform，复杂场景（如宿主元素）将在P2实现recreate策略";
        }

        /// <summary>
        /// 检查元素是否支持directTransform移动
        /// </summary>
        private bool CanMoveElement(Element elem)
        {
            // P1阶段：仅允许移动独立族实例和基本系统族
            if (elem is FamilyInstance fi)
            {
                // 检查是否有宿主
                return fi.Host == null;
            }

            // 墙、楼板等系统族可移动
            if (elem is Wall || elem is Floor)
            {
                return true;
            }

            // 其他类型暂不支持
            return false;
        }
    }
}