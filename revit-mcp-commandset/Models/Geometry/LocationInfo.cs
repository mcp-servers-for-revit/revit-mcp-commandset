using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Geometry;

/// <summary>
/// 统一的位置信息类
/// 序列化时只会包含point或line其中一个
/// </summary>
public class LocationInfo
{
    /// <summary>
    /// 点位置信息
    /// </summary>
    [JsonProperty("point", NullValueHandling = NullValueHandling.Ignore)]
    public JZPoint Point { get; set; }

    /// <summary>
    /// 线位置信息
    /// </summary>
    [JsonProperty("line", NullValueHandling = NullValueHandling.Ignore)]
    public JZLine Line { get; set; }
}