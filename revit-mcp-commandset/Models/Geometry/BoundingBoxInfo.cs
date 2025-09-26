using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Geometry;

/// <summary>
/// 包围盒信息类
/// </summary>
public class BoundingBoxInfo
{
    /// <summary>
    /// 最小点
    /// </summary>
    [JsonProperty("min")]
    public JZPoint Min { get; set; }

    /// <summary>
    /// 最大点
    /// </summary>
    [JsonProperty("max")]
    public JZPoint Max { get; set; }
}