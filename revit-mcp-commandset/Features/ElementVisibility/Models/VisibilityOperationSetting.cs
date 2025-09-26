using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitMCPCommandSet.Features.ElementVisibility.Models
{
    /// <summary>
    /// Visibility 操作设置
    /// 支持操作：Hide, TempHide, Isolate, Unhide, ResetIsolate
    /// </summary>
    public class VisibilityOperationSetting
    {
        /// <summary>
        /// 需要操作的元素ID列表（ResetIsolate时可为空）
        /// </summary>
        [JsonProperty("elementIds")]
        public List<int> ElementIds { get; set; }

        /// <summary>
        /// 可见性操作类型
        /// </summary>
        [JsonProperty("visibilityAction")]
        public string VisibilityAction { get; set; }

        /// <summary>
        /// 验证参数
        /// </summary>
        public void Validate()
        {
            // 1. visibilityAction 验证
            var validActions = new[] { "Hide", "TempHide", "Isolate", "Unhide", "ResetIsolate" };
            if (string.IsNullOrEmpty(VisibilityAction) || !validActions.Contains(VisibilityAction))
                throw new ArgumentException($"不支持的操作: {VisibilityAction}，支持的操作: {string.Join(", ", validActions)}");

            // 2. elementIds 条件验证（ResetIsolate 允许为空）
            if (VisibilityAction != "ResetIsolate" && (ElementIds == null || ElementIds.Count == 0))
                throw new ArgumentException($"{VisibilityAction} 操作需要提供 elementIds");
        }
    }
}