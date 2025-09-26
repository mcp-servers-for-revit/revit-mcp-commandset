using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Features.ElementVisual.Models
{
    /// <summary>
    /// Visual 操作设置
    /// 支持操作：Select, SelectionBox, Highlight, SetColor, SetTransparency
    /// </summary>
    public class VisualOperationSetting
    {
        /// <summary>
        /// 需要操作的元素ID列表
        /// </summary>
        [JsonProperty("elementIds")]
        public List<int> ElementIds { get; set; }

        /// <summary>
        /// 视觉操作类型
        /// </summary>
        [JsonProperty("visualAction")]
        public string VisualAction { get; set; }

        /// <summary>
        /// 颜色值（RGB格式），默认红色
        /// </summary>
        [JsonProperty("colorValue")]
        public int[] ColorValue { get; set; }

        /// <summary>
        /// 透明度值(0-100)，数值越大透明度越高，默认50
        /// 使用可空类型以区分"未设置"和"明确设为0"
        /// </summary>
        [JsonProperty("transparencyValue")]
        public int? TransparencyValue { get; set; }

        /// <summary>
        /// 验证并规范化参数
        /// </summary>
        public void Validate()
        {
            // 1. elementIds 验证
            if (ElementIds == null || ElementIds.Count == 0)
                throw new ArgumentException("elementIds 不能为空");

            // 2. visualAction 验证
            var validActions = new[] { "Select", "SelectionBox", "Highlight", "SetColor", "SetTransparency" };
            if (string.IsNullOrEmpty(VisualAction) || !validActions.Contains(VisualAction))
                throw new ArgumentException($"不支持的操作: {VisualAction}，支持的操作: {string.Join(", ", validActions)}");

            // 3. colorValue 规范化
            if (ColorValue == null || ColorValue.Length < 3)
            {
                ColorValue = new int[] { 255, 0, 0 }; // 默认红色
            }
            else
            {
                // 裁剪到合法范围
                for (int i = 0; i < Math.Min(ColorValue.Length, 3); i++)
                {
                    ColorValue[i] = Math.Max(0, Math.Min(255, ColorValue[i]));
                }
                // 确保数组长度为3
                if (ColorValue.Length > 3)
                {
                    ColorValue = new int[] { ColorValue[0], ColorValue[1], ColorValue[2] };
                }
            }

            // 4. transparencyValue 规范化
            if (VisualAction == "SetTransparency")
            {
                // 仅在真正未提供透明度值时使用默认值50
                if (!TransparencyValue.HasValue)
                {
                    TransparencyValue = 50;
                }
                else
                {
                    // 将值限制在0-100范围内
                    TransparencyValue = Math.Max(0, Math.Min(100, TransparencyValue.Value));
                }
            }
        }
    }
}