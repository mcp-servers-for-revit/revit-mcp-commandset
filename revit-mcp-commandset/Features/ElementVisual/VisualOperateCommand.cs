using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementVisual.Models;
using System;

namespace RevitMCPCommandSet.Features.ElementVisual
{
    /// <summary>
    /// Visual 操作命令 - 纯视觉效果，不改变模型几何或属性
    /// 支持操作：Select, SelectionBox, Highlight, SetColor, SetTransparency
    /// </summary>
    public class VisualOperateCommand : ExternalEventCommandBase
    {
        private VisualOperateEventHandler _handler => (VisualOperateEventHandler)Handler;

        public override string CommandName => "operate_element_visual";

        public VisualOperateCommand(UIApplication uiApp)
            : base(new VisualOperateEventHandler(), uiApp) { }

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
                var setting = dataToken.ToObject<VisualOperationSetting>();
                if (setting == null)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "参数解析失败：无法解析 VisualOperationSetting",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // 3. 参数验证（在这里进行初步验证，详细验证在 EventHandler 中）
                if (setting.ElementIds == null || setting.ElementIds.Count == 0)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "elementIds 不能为空",
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
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.Result;
                }
                else
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "操作超时（10秒）",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = setting.ElementIds.Count,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = setting.ElementIds.ConvertAll(id => new FailureInfo
                            {
                                ElementId = id,
                                Reason = "操作超时"
                            })
                        }
                    };
                }
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