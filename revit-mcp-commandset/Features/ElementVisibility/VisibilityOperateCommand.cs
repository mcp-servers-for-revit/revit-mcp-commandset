using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementVisibility.Models;
using System;

namespace RevitMCPCommandSet.Features.ElementVisibility
{
    /// <summary>
    /// Visibility 操作命令 - 当前视图可见性管理
    /// 支持操作：Hide, TempHide, Isolate, Unhide, ResetIsolate
    /// </summary>
    public class VisibilityOperateCommand : ExternalEventCommandBase
    {
        private VisibilityOperateEventHandler _handler => (VisibilityOperateEventHandler)Handler;

        public override string CommandName => "operate_element_visibility";

        public VisibilityOperateCommand(UIApplication uiApp)
            : base(new VisibilityOperateEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // 1. 强制解析 data 包裹层
                var dataToken = parameters["data"];
                if (dataToken == null)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "参数格式错误：缺少 'data' 包裹层",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // 2. 解析实际业务参数
                var setting = dataToken.ToObject<VisibilityOperationSetting>();
                if (setting == null)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "参数解析失败：无法解析 VisibilityOperationSetting",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // 3. 参数验证（在这里进行初步验证，详细验证在 EventHandler 中）
                if (string.IsNullOrEmpty(setting.VisibilityAction))
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "visibilityAction 不能为空",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // ResetIsolate 操作允许 elementIds 为空，其他操作需要验证
                if (setting.VisibilityAction != "ResetIsolate" &&
                    (setting.ElementIds == null || setting.ElementIds.Count == 0))
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = $"{setting.VisibilityAction} 操作需要提供 elementIds",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // 4. 设置 Handler 参数
                _handler.SetParameters(setting);

                // 5. 触发异步事件并等待完成
                bool completed = RaiseAndWaitForCompletion(10000);
                if (!completed)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "操作超时（10秒）",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = setting.ElementIds?.Count ?? 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = setting.ElementIds?.ConvertAll(id => new FailureInfo
                            {
                                ElementId = id,
                                Reason = "操作超时"
                            }) ?? new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                return _handler.Result;
            }
            catch (Exception ex)
            {
                return new AIResult<ElementOperationResponse>
                {
                    Success = false,
                    Message = $"命令执行异常: {ex.Message}",
                    Response = new ElementOperationResponse
                    {
                        ProcessedCount = 0,
                        SuccessfulElements = new System.Collections.Generic.List<int>(),
                        FailedElements = new System.Collections.Generic.List<FailureInfo>()
                    }
                };
            }
        }
    }
}