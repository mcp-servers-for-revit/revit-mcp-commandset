using Autodesk.Revit.UI;
using Newtonsoft.Json.Linq;
using RevitMCPSDK.API.Base;
using RevitMCPCommandSet.Models.Common;
using System;

namespace RevitMCPCommandSet.Features.RevitStatus
{
    /// <summary>
    /// 获取Revit状态命令
    /// </summary>
    public class GetRevitStatusCommand : ExternalEventCommandBase
    {
        private GetRevitStatusEventHandler _handler => (GetRevitStatusEventHandler)Handler;

        /// <summary>
        /// 命令名称
        /// </summary>
        public override string CommandName => "get_revit_status";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="uiApp">Revit UIApplication</param>
        public GetRevitStatusCommand(UIApplication uiApp)
            : base(new GetRevitStatusEventHandler(), uiApp)
        {
        }

        public override object Execute(JObject parameters, string requestId)
        {
            try
            {
                // 强制解析 data 包裹层
                var dataToken = parameters["data"];
                if (dataToken == null)
                {
                    return new AIResult<RevitStatusInfo>
                    {
                        Success = false,
                        Message = "参数格式错误：缺少 'data' 包裹层"
                    };
                }

                // 此命令不需要输入参数，但仍需要data包裹层
                // 可以传入空的data对象 {}

                // 触发外部事件并等待完成
                if (RaiseAndWaitForCompletion(10000))
                {
                    return _handler.Result;
                }
                else
                {
                    throw new TimeoutException("获取Revit状态操作超时");
                }
            }
            catch (Exception ex)
            {
                return new AIResult<RevitStatusInfo>
                {
                    Success = false,
                    Message = $"获取Revit状态失败: {ex.Message}",
                    Response = null
                };
            }
        }
    }
}