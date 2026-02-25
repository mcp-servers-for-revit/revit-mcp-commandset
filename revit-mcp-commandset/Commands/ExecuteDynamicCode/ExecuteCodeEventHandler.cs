using System.CodeDom.Compiler;
using Autodesk.Revit.UI;
using Microsoft.CSharp;
using Newtonsoft.Json;
using RevitMCPSDK.API.Interfaces;

namespace RevitMCPCommandSet.Commands.ExecuteDynamicCode
{
    /// <summary>
    /// 处理代码执行的外部事件处理器
    /// </summary>
    public class ExecuteCodeEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
    {
        // 代码执行参数
        private string _generatedCode;
        private object[] _executionParameters;

        // 执行结果信息
        public ExecutionResultInfo ResultInfo { get; private set; }

        // 状态同步对象
        public bool TaskCompleted { get; private set; }
        private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

        // 设置要执行的代码和参数
        public void SetExecutionParameters(string code, object[] parameters = null)
        {
            _generatedCode = code;
            _executionParameters = parameters ?? Array.Empty<object>();
            TaskCompleted = false;
            _resetEvent.Reset();
        }

        // 等待执行完成 - IWaitableExternalEventHandler接口实现
        public bool WaitForCompletion(int timeoutMilliseconds = 10000)
        {
            return _resetEvent.WaitOne(timeoutMilliseconds);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                var doc = app.ActiveUIDocument.Document;
                ResultInfo = new ExecutionResultInfo();

                using (var transaction = new Transaction(doc, "执行AI代码"))
                {
                    transaction.Start();

                    // 动态编译执行代码
                    var result = CompileAndExecuteCode(
                        code: _generatedCode,
                        doc: doc,
                        parameters: _executionParameters
                    );

                    transaction.Commit();

                    ResultInfo.Success = true;
                    ResultInfo.Result = JsonConvert.SerializeObject(result);
                }
            }
            catch (Exception ex)
            {
                ResultInfo.Success = false;
                ResultInfo.ErrorMessage = $"执行失败: {ex.Message}";
            }
            finally
            {
                TaskCompleted = true;
                _resetEvent.Set();
            }
        }

        private object CompileAndExecuteCode(string code, Document doc, object[] parameters)
        {
            // 添加必要的程序集引用
            var compilerParams = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                ReferencedAssemblies =
                {
                    "System.dll",
                    "System.Core.dll",
                    typeof(Document).Assembly.Location,  // RevitAPI.dll
                    typeof(UIApplication).Assembly.Location // RevitAPIUI.dll
                }
            };

            // 包装代码以规范入口点
            var wrappedCode = $@"
using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace AIGeneratedCode
{{
    public static class CodeExecutor
    {{
        public static object Execute(Document document, object[] parameters)
        {{
            // Convenience aliases matching common Revit API patterns
            var Document = document;
            var Doc = document;
            var doc = document;
            {code}
        }}
    }}
}}";

            // 编译代码
            using (var provider = new CSharpCodeProvider())
            {
                var compileResults = provider.CompileAssemblyFromSource(
                    compilerParams,
                    wrappedCode
                );

                // 处理编译结果
                if (compileResults.Errors.HasErrors)
                {
                    var errors = string.Join("\n", compileResults.Errors
                        .Cast<CompilerError>()
                        .Select(e => $"Line {e.Line}: {e.ErrorText}"));
                    throw new Exception($"代码编译错误:\n{errors}");
                }

                // 反射调用执行方法
                var assembly = compileResults.CompiledAssembly;
                var executorType = assembly.GetType("AIGeneratedCode.CodeExecutor");
                var executeMethod = executorType.GetMethod("Execute");

                return executeMethod.Invoke(null, new object[] { doc, parameters });
            }
        }

        public string GetName()
        {
            return "执行AI代码";
        }
    }

    // 执行结果数据结构
    public class ExecutionResultInfo
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}
