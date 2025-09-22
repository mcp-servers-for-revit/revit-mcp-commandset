using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils.FamilyCreation;

namespace RevitMCPCommandSet.Features.FamilyInstanceCreation
{
    /// <summary>
    /// 获取族创建建议事件处理器
    /// </summary>
    public class GetFamilyCreationSuggestionEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        /// <summary>
        /// 事件等待对象
        /// </summary>
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        /// <summary>
        /// 族类型ID（传入数据）
        /// </summary>
        private int typeId;

        /// <summary>
        /// 执行结果（传出数据）
        /// </summary>
        public AIResult<FamilyCreationRequirements> Result { get; private set; }

        /// <summary>
        /// 设置参数
        /// </summary>
        public void SetParameters(int id)
        {
            typeId = id;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                // 获取族类型
                var symbol = doc.GetElement(new ElementId(typeId)) as FamilySymbol;

                if (symbol == null)
                {
                    Result = new AIResult<FamilyCreationRequirements>
                    {
                        Success = false,
                        Message = $"无效的族类型ID: {typeId}",
                        Response = null
                    };
                }
                else
                {
                    // 使用FamilyInstanceService分析创建参数需求
                    var service = new FamilyInstanceService(doc);
                    var requirements = service.AnalyzeRequirements(typeId);

                    Result = new AIResult<FamilyCreationRequirements>
                    {
                        Success = true,
                        Response = requirements,
                        Message = "成功获取族创建参数需求"
                    };
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<FamilyCreationRequirements>
                {
                    Success = false,
                    Message = $"分析异常: {ex.Message}",
                    Response = null
                };
                System.Diagnostics.Trace.WriteLine($"[GetFamilyCreationSuggestion] 异常详情: {ex}");
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
            return "GetFamilyCreationSuggestionEventHandler";
        }
    }
}