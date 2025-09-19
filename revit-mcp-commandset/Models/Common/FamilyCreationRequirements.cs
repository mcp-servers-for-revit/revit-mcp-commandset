using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Common
{
    /// <summary>
    /// 族创建参数需求
    /// </summary>
    public class FamilyCreationRequirements
    {
        /// <summary>
        /// 族类型ElementId
        /// </summary>
        [JsonProperty("typeId")]
        public int TypeId { get; set; }

        /// <summary>
        /// 族名称
        /// </summary>
        [JsonProperty("familyName")]
        public string FamilyName { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        [JsonProperty("typeName")]
        public string TypeName { get; set; }

        /// <summary>
        /// 放置类型
        /// </summary>
        [JsonProperty("placementType")]
        public string PlacementType { get; set; }

        /// <summary>
        /// 是否支持
        /// </summary>
        [JsonProperty("isSupported")]
        public bool IsSupported { get; set; }

        /// <summary>
        /// 不支持的原因
        /// </summary>
        [JsonProperty("unsupportedReason")]
        public string UnsupportedReason { get; set; }

        /// <summary>
        /// 必需参数
        /// </summary>
        [JsonProperty("requiredParameters")]
        public Dictionary<string, ParameterInfo> RequiredParameters { get; set; }

        /// <summary>
        /// 可选参数
        /// </summary>
        [JsonProperty("optionalParameters")]
        public Dictionary<string, ParameterInfo> OptionalParameters { get; set; }

        /// <summary>
        /// 示例值
        /// </summary>
        [JsonProperty("examples")]
        public Dictionary<string, object> Examples { get; set; }

        public FamilyCreationRequirements()
        {
            RequiredParameters = new Dictionary<string, ParameterInfo>();
            OptionalParameters = new Dictionary<string, ParameterInfo>();
            Examples = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// 参数信息
    /// </summary>
    public class ParameterInfo
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 单位约定
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// 参数说明
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// 示例值
        /// </summary>
        [JsonProperty("example")]
        public object Example { get; set; }

        /// <summary>
        /// 取值范围（可选）
        /// </summary>
        [JsonProperty("range")]
        public string Range { get; set; }
    }
}