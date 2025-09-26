using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementVisibility.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RevitMCPCommandSet.Features.ElementVisibility
{
    public class VisibilityOperateEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
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
        public VisibilityOperationSetting OperationData { get; private set; }

        /// <summary>
        /// 执行结果（传出数据）
        /// </summary>
        public AIResult<ElementOperationResponse> Result { get; private set; }

        /// <summary>
        /// 设置操作参数
        /// </summary>
        public void SetParameters(VisibilityOperationSetting data)
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

                // 执行可见性操作
                var result = ExecuteVisibilityOperation(uiDoc, OperationData);

                Result = result;
            }
            catch (Exception ex)
            {
                Result = new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"可见性操作失败: {ex.Message}",
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
            return "Visibility操作元素";
        }

        /// <summary>
        /// 执行可见性操作（复用现有逻辑）
        /// </summary>
        private AIResult<ElementOperationResponse> ExecuteVisibilityOperation(
            UIDocument uidoc,
            VisibilityOperationSetting setting)
        {
            var successfulElements = new List<int>();
            var failedElements = new List<FailureInfo>();
            var details = new Dictionary<string, object>();

            try
            {
                // 根据操作类型执行不同的操作
                switch (setting.VisibilityAction)
                {
                    case "Hide":
                        // 隐藏元素（需要活动视图和事务）
                        using (Transaction trans = new Transaction(doc, "隐藏元素"))
                        {
                            trans.Start();
                            var elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();
                            doc.ActiveView.HideElements(elementIds);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        break;

                    case "TempHide":
                        // 临时隐藏元素（需要活动视图和事务）
                        using (Transaction trans = new Transaction(doc, "临时隐藏元素"))
                        {
                            trans.Start();
                            var elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();
                            doc.ActiveView.HideElementsTemporary(elementIds);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        break;

                    case "Isolate":
                        // 隔离元素（需要活动视图和事务）
                        using (Transaction trans = new Transaction(doc, "隔离元素"))
                        {
                            trans.Start();
                            var elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();
                            doc.ActiveView.IsolateElementsTemporary(elementIds);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        break;

                    case "Unhide":
                        // 取消隐藏元素（需要活动视图和事务）
                        using (Transaction trans = new Transaction(doc, "取消隐藏元素"))
                        {
                            trans.Start();
                            var elementIds = setting.ElementIds.Select(id => new ElementId(id)).ToList();
                            doc.ActiveView.UnhideElements(elementIds);
                            trans.Commit();
                        }
                        successfulElements.AddRange(setting.ElementIds);
                        break;

                    case "ResetIsolate":
                        // 重置隔离（需要活动视图和事务）
                        using (Transaction trans = new Transaction(doc, "重置隔离"))
                        {
                            trans.Start();
                            doc.ActiveView.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                            trans.Commit();
                        }
                        // ResetIsolate 不需要具体的元素ID，设置为空列表
                        successfulElements = new List<int>();
                        details["globalAction"] = true;
                        break;

                    default:
                        throw new Exception($"未支持的操作类型：{setting.VisibilityAction}");
                }

                // 构造成功消息
                string message;
                if (setting.VisibilityAction == "ResetIsolate")
                {
                    message = "成功重置隔离模式";
                }
                else
                {
                    message = $"成功对 {successfulElements.Count} 个元素执行 {setting.VisibilityAction} 操作";
                }

                return new AIResult<ElementOperationResponse>
                {
                    Success = true,
                    Message = message,
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = setting.VisibilityAction == "ResetIsolate" ? 0 : (setting.ElementIds?.Count ?? 0),
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
                    Message = $"可见性操作失败: {ex.Message}",
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = setting.ElementIds?.Count ?? 0,
                        SuccessfulElements = successfulElements,
                        FailedElements = setting.ElementIds?.Select(id => new FailureInfo
                        {
                            ElementId = id,
                            Reason = ex.Message
                        }).ToList() ?? new List<FailureInfo>(),
                        Details = details
                    }
                };
            }
        }
    }
}