using Newtonsoft.Json;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// 元素操作响应数据（统一格式）
    /// 用于 AIResult&lt;ElementOperationResponse&gt; 的 Response 字段
    /// </summary>
    public class ElementOperationResponse
    {
        /// <summary>
        /// 处理的元素总数
        /// </summary>
        [JsonProperty("processedCount")]
        public int ProcessedCount { get; set; }

        /// <summary>
        /// 成功处理的元素ID列表
        /// </summary>
        [JsonProperty("successfulElements")]
        public List<int> SuccessfulElements { get; set; } = new List<int>();

        /// <summary>
        /// 失败的元素及其失败原因
        /// </summary>
        [JsonProperty("failedElements")]
        public List<FailureInfo> FailedElements { get; set; } = new List<FailureInfo>();

        /// <summary>
        /// 命令特定的详细数据
        /// </summary>
        [JsonProperty("details")]
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
    }

    /// <summary>
    /// 失败元素信息
    /// </summary>
    public class FailureInfo
    {
        /// <summary>
        /// 失败的元素ID
        /// </summary>
        [JsonProperty("elementId")]
        public int ElementId { get; set; }

        /// <summary>
        /// 失败原因
        /// </summary>
        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}