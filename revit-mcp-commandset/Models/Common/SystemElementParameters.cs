using RevitMCPCommandSet.Models.Geometry;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// 系统族创建参数（组合模式，适配MCP架构）
    /// </summary>
    public class SystemElementParameters
    {
        /// <summary>
        /// 系统族类型（字符串形式：wall, floor, ceiling, roof）
        /// </summary>
        [JsonProperty("elementType")]
        public string ElementType { get; set; }

        /// <summary>
        /// 系统族类型ID (WallType或FloorType的ElementId)
        /// </summary>
        [JsonProperty("typeId")]
        public int TypeId { get; set; }

        /// <summary>
        /// 关联标高ID（可选）
        /// </summary>
        [JsonProperty("levelId")]
        public int? LevelId { get; set; }

        /// <summary>
        /// 自动查找最近标高
        /// </summary>
        [JsonProperty("autoFindLevel")]
        public bool AutoFindLevel { get; set; } = true;

        // === 墙体专用参数 ===

        /// <summary>
        /// 墙体路径（起点和终点，单位：毫米）- 墙体必需
        /// </summary>
        [JsonProperty("wallLine")]
        public JZLine WallLine { get; set; }

        /// <summary>
        /// 墙体高度（毫米）- 墙体必需
        /// </summary>
        [JsonProperty("height")]
        public double? Height { get; set; }

        /// <summary>
        /// 底部偏移（毫米）- 墙体可选
        /// </summary>
        [JsonProperty("baseOffset")]
        public double BaseOffset { get; set; } = 0;

        /// <summary>
        /// 自动连接相邻墙体 - 墙体可选
        /// </summary>
        [JsonProperty("autoJoinWalls")]
        public bool AutoJoinWalls { get; set; } = true;

        // === 楼板专用参数 ===

        /// <summary>
        /// 楼板边界点列表（按顺序连接形成闭合轮廓，单位：毫米）- 楼板必需
        /// </summary>
        [JsonProperty("floorBoundary")]
        public List<JZPoint> FloorBoundary { get; set; }

        /// <summary>
        /// 楼板顶部偏移（毫米）- 楼板可选
        /// </summary>
        [JsonProperty("topOffset")]
        public double TopOffset { get; set; } = 0;

        /// <summary>
        /// 楼板坡度（可选，默认为null表示水平楼板）- 楼板可选
        /// </summary>
        [JsonProperty("slope")]
        public double? Slope { get; set; }

        // === 通用可选参数 ===

        /// <summary>
        /// 是否结构构件（墙体和楼板通用）
        /// </summary>
        [JsonProperty("isStructural")]
        public bool IsStructural { get; set; } = false;
    }
}