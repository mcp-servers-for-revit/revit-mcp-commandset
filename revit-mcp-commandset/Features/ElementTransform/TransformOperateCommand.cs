using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementTransform.Models;
using System;

namespace RevitMCPCommandSet.Features.ElementTransform
{
    /// <summary>
    /// Transform 操作命令 - 几何变换
    /// 支持操作：Rotate, Mirror, Flip, Move
    /// </summary>
    public class TransformOperateCommand : ExternalEventCommandBase
    {
        private TransformOperateEventHandler _handler => (TransformOperateEventHandler)Handler;

        public override string CommandName => "operate_element_transform";

        public TransformOperateCommand(UIApplication uiApp)
            : base(new TransformOperateEventHandler(), uiApp) { }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
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

                var setting = dataToken.ToObject<TransformOperationSetting>();
                if (setting == null)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "参数解析失败：无法解析 TransformOperationSetting",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                setting.Validate();
                _handler.SetParameters(setting);

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