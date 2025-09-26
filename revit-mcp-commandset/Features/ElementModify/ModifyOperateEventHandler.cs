using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementModify.Models;
using RevitMCPCommandSet.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Features.ElementModify
{
    public class ModifyOperateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        public ModifyOperationSetting OperationData { get; private set; }
        public AIResult<ElementOperationResponse> Result { get; private set; }

        public void SetParameters(ModifyOperationSetting data)
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
                var result = ExecuteModifyOperation(uiDoc, OperationData);
                Result = result;
            }
            catch (Exception ex)
            {
                Result = new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"修改操作失败: {ex.Message}",
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
            return "Modify操作元素";
        }

        private AIResult<ElementOperationResponse> ExecuteModifyOperation(
            UIDocument uidoc,
            ModifyOperationSetting setting)
        {
            var successfulElements = new List<int>();
            var failedElements = new List<FailureInfo>();
            var details = new Dictionary<string, object>();

            ICollection<ElementId> elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();

            try
            {
                switch (setting.ModifyAction)
                {
                    case "SetParameter":
                        // 参数修改操作
                        using (Transaction trans = new Transaction(doc, "修改参数"))
                        {
                            trans.Start();

                            foreach (var elemId in elementIds)
                            {
                                Element elem = doc.GetElement(elemId);
                                if (elem == null)
                                {
                                    failedElements.Add(new FailureInfo
                                    {
                                        ElementId = elemId.IntegerValue,
                                        Reason = "元素不存在"
                                    });
                                    continue;
                                }

                                var (success, errorMessage) = ParameterHelper.SetParameter(
                                    elem,
                                    setting.ParameterName,
                                    setting.ParameterValue,
                                    setting.IsBuiltInParameter,
                                    setting.ParameterType
                                );

                                if (success)
                                {
                                    successfulElements.Add(elemId.IntegerValue);
                                }
                                else
                                {
                                    failedElements.Add(new FailureInfo
                                    {
                                        ElementId = elemId.IntegerValue,
                                        Reason = errorMessage
                                    });
                                }
                            }

                            trans.Commit();
                        }

                        details["parameterName"] = setting.ParameterName;
                        details["parameterValue"] = setting.ParameterValue;
                        break;

                    case "Delete":
                        // 删除操作
                        using (Transaction trans = new Transaction(doc, "删除元素"))
                        {
                            trans.Start();

                            foreach (var elemId in elementIds)
                            {
                                try
                                {
                                    ICollection<ElementId> deletedIds = doc.Delete(elemId);
                                    if (deletedIds != null && deletedIds.Count > 0)
                                    {
                                        successfulElements.Add(elemId.IntegerValue);
                                    }
                                    else
                                    {
                                        failedElements.Add(new FailureInfo
                                        {
                                            ElementId = elemId.IntegerValue,
                                            Reason = "删除失败，未返回已删除ID"
                                        });
                                    }
                                }
                                catch (Exception ex)
                                {
                                    failedElements.Add(new FailureInfo
                                    {
                                        ElementId = elemId.IntegerValue,
                                        Reason = ex.Message
                                    });
                                }
                            }

                            trans.Commit();
                        }
                        break;

                    default:
                        throw new Exception($"未支持的操作类型：{setting.ModifyAction}");
                }

                return new AIResult<ElementOperationResponse>
                {
                    Success = failedElements.Count == 0,
                    Message = failedElements.Count == 0
                        ? $"成功对 {successfulElements.Count} 个元素执行 {setting.ModifyAction} 操作"
                        : $"{setting.ModifyAction} 操作部分失败：成功 {successfulElements.Count} 个，失败 {failedElements.Count} 个",
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
                    Message = $"修改操作失败: {ex.Message}",
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
    }
}