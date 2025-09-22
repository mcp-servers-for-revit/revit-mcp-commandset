using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common;

/// <summary>
/// 创建结果
/// </summary>
public class CreateResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    [JsonProperty("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 消息
    /// </summary>
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// 创建的元素ID
    /// </summary>
    [JsonProperty("elementId")]
    public int ElementId { get; set; } = -1;

    /// <summary>
    /// 附加信息
    /// </summary>
    [JsonProperty("additionalInfo")]
    public Dictionary<string, object> AdditionalInfo { get; set; }

    public CreateResult()
    {
        AdditionalInfo = new Dictionary<string, object>();
    }
}