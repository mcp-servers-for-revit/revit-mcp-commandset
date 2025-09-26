using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Features.ElementModify.Models;
using System;

namespace RevitMCPCommandSet.Features.ElementModify
{
    /// <summary>
    /// Modify 操作命令 - 参数修改与删除
    /// 支持操作：SetParameter, Delete
    /// </summary>
    public class ModifyOperateCommand : ExternalEventCommandBase
    {
        private ModifyOperateEventHandler _handler => (ModifyOperateEventHandler)Handler;

        public override string CommandName => "operate_element_modify";

        public ModifyOperateCommand(UIApplication uiApp)
            : base(new ModifyOperateEventHandler(), uiApp) { }

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
                var setting = dataToken.ToObject<ModifyOperationSetting>();
                if (setting == null)
                {
                    return new AIResult<ElementOperationResponse>
                    {
                        Success = false,
                        Message = "参数解析失败：无法解析 ModifyOperationSetting",
                        Response = new ElementOperationResponse
                        {
                            ProcessedCount = 0,
                            SuccessfulElements = new System.Collections.Generic.List<int>(),
                            FailedElements = new System.Collections.Generic.List<FailureInfo>()
                        }
                    };
                }

                // 3. 参数验证
                setting.Validate();

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