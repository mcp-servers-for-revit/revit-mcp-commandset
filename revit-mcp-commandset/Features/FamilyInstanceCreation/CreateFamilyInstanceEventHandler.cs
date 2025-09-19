using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitMCPSDK.API.Interfaces;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Features.FamilyInstanceCreation
{
    /// <summary>
    /// 创建族实例事件处理器
    /// </summary>
    public class CreateFamilyInstanceEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        private UIApplication uiApp;
        private UIDocument uiDoc => uiApp.ActiveUIDocument;
        private Document doc => uiDoc.Document;

        /// <summary>
        /// 事件等待对象
        /// </summary>
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        /// <summary>
        /// 创建参数（传入数据）
        /// </summary>
        private FamilyCreationParameters parameters;

        /// <summary>
        /// 执行结果（传出数据）
        /// </summary>
        public AIResult<CreateResult> Result { get; private set; }

        /// <summary>
        /// 设置创建参数
        /// </summary>
        public void SetParameters(FamilyCreationParameters param)
        {
            parameters = param;
            _resetEvent.Reset();
        }

        public void Execute(UIApplication uiapp)
        {
            uiApp = uiapp;

            try
            {
                // 使用事务包装创建过程
                using (Transaction trans = new Transaction(doc, "Create Family Instance"))
                {
                    trans.Start();

                    // 使用SafeFamilyInstanceCreator创建实例
                    var creator = new SafeFamilyInstanceCreator(doc);
                    var createResult = creator.CreateInstance(parameters);

                    if (createResult.Success)
                    {
                        trans.Commit();
                        Result = new AIResult<CreateResult>
                        {
                            Success = true,
                            Response = createResult,
                            Message = createResult.Message
                        };
                    }
                    else
                    {
                        trans.RollBack();
                        Result = new AIResult<CreateResult>
                        {
                            Success = false,
                            Response = createResult,
                            Message = createResult.Message
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Result = new AIResult<CreateResult>
                {
                    Success = false,
                    Message = $"事务异常: {ex.Message}",
                    Response = new CreateResult
                    {
                        Success = false,
                        Message = $"事务异常: {ex.Message}"
                    }
                };
                System.Diagnostics.Trace.WriteLine($"[CreateFamilyInstance] 异常详情: {ex}");
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
            return "CreateFamilyInstanceEventHandler";
        }
    }
}