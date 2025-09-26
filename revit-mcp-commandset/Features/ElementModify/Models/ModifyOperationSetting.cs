using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace RevitMCPCommandSet.Features.ElementModify.Models
{
    /// <summary>
    /// 元素修改操作参数设置
    /// </summary>
    public class ModifyOperationSetting
    {
        /// <summary>
        /// 目标元素ID数组
        /// </summary>
        [JsonProperty("elementIds")]
        public List<int> ElementIds { get; set; }

        /// <summary>
        /// 修改操作类型（枚举：SetParameter, Delete）
        /// </summary>
        [JsonProperty("modifyAction")]
        public string ModifyAction { get; set; }

        /// <summary>
        /// SetParameter 专用：参数名称
        /// </summary>
        [JsonProperty("parameterName")]
        public string ParameterName { get; set; }

        /// <summary>
        /// SetParameter 专用：参数值（支持字符串、数字、布尔）
        /// </summary>
        [JsonProperty("parameterValue")]
        public object ParameterValue { get; set; }

        /// <summary>
        /// SetParameter 专用：是否为内置参数（BuiltInParameter）
        /// 默认false，表示按名称查找
        /// </summary>
        [JsonProperty("isBuiltInParameter")]
        public bool IsBuiltInParameter { get; set; } = false;

        /// <summary>
        /// SetParameter 专用：参数类型提示（可选）
        /// 用于辅助类型转换：String, Double, Integer, ElementId
        /// </summary>
        [JsonProperty("parameterType")]
        public string ParameterType { get; set; }

        /// <summary>
        /// 参数验证
        /// </summary>
        public void Validate()
        {
            // 基本验证
            if (string.IsNullOrEmpty(ModifyAction))
            {
                throw new ArgumentException("modifyAction 不能为空");
            }

            if (ElementIds == null || ElementIds.Count == 0)
            {
                throw new ArgumentException("elementIds 不能为空");
            }

            // SetParameter 操作的特殊验证
            if (ModifyAction == "SetParameter")
            {
                if (string.IsNullOrEmpty(ParameterName))
                {
                    throw new ArgumentException("SetParameter 操作需要提供 parameterName");
                }

                if (ParameterValue == null)
                {
                    throw new ArgumentException("SetParameter 操作需要提供 parameterValue");
                }
            }

            // 验证操作类型有效性
            var validActions = new[] { "SetParameter", "Delete" };
            if (Array.IndexOf(validActions, ModifyAction) == -1)
            {
                throw new ArgumentException(
                    $"不支持的操作: {ModifyAction}，支持的操作: {string.Join(", ", validActions)}"
                );
            }
        }
    }
}