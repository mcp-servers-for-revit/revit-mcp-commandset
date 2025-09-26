using Autodesk.Revit.DB;
using System;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 参数处理辅助类
    /// 复用并增强 ProjectUtils.EditParameter 逻辑
    /// </summary>
    public static class ParameterHelper
    {
        /// <summary>
        /// 设置元素参数值
        /// </summary>
        /// <param name="element">目标元素</param>
        /// <param name="parameterName">参数名称</param>
        /// <param name="value">参数值</param>
        /// <param name="isBuiltIn">是否为内置参数</param>
        /// <param name="parameterType">参数类型提示（可选）</param>
        /// <returns>成功返回true，失败返回false及原因</returns>
        public static (bool Success, string ErrorMessage) SetParameter(
            Element element,
            string parameterName,
            object value,
            bool isBuiltIn = false,
            string parameterType = null)
        {
            try
            {
                Parameter param = null;

                // 1. 查找参数
                if (isBuiltIn)
                {
                    // 内置参数查找
                    if (Enum.TryParse(parameterName, out BuiltInParameter builtInParam))
                    {
                        param = element.get_Parameter(builtInParam);
                    }
                    else
                    {
                        return (false, $"无效的内置参数名称: {parameterName}");
                    }
                }
                else
                {
                    // 按名称查找（优先实例参数，再查类型参数）
                    param = element.LookupParameter(parameterName);
                }

                if (param == null)
                {
                    return (false, $"参数不存在: {parameterName}");
                }

                // 2. 检查只读
                if (param.IsReadOnly)
                {
                    return (false, $"参数为只读: {parameterName}");
                }

                // 3. 根据参数存储类型设置值
                switch (param.StorageType)
                {
                    case StorageType.String:
                        param.Set(value?.ToString() ?? "");
                        break;

                    case StorageType.Double:
                        // 数值类型（长度、角度等）
                        double doubleValue = ConvertToDouble(value);

                        // 单位转换：如果是长度参数，从毫米转换为英尺
                        if (IsLengthParameter(param))
                        {
                            doubleValue = doubleValue / 304.8; // mm -> feet
                        }
                        // 如果是角度参数，从度转换为弧度
                        else if (IsAngleParameter(param))
                        {
                            doubleValue = doubleValue * Math.PI / 180.0; // degree -> radian
                        }

                        param.Set(doubleValue);
                        break;

                    case StorageType.Integer:
                        int intValue = ConvertToInt(value);
                        param.Set(intValue);
                        break;

                    case StorageType.ElementId:
                        ElementId elemId = ConvertToElementId(value);
                        param.Set(elemId);
                        break;

                    default:
                        return (false, $"不支持的参数存储类型: {param.StorageType}");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"设置参数失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 判断是否为长度参数
        /// </summary>
        private static bool IsLengthParameter(Parameter param)
        {
#if REVIT2021_OR_GREATER
            return param.Definition.GetDataType() == SpecTypeId.Length;
#else
            return param.Definition.UnitType == UnitType.UT_Length;
#endif
        }

        /// <summary>
        /// 判断是否为角度参数
        /// </summary>
        private static bool IsAngleParameter(Parameter param)
        {
#if REVIT2021_OR_GREATER
            return param.Definition.GetDataType() == SpecTypeId.Angle;
#else
            return param.Definition.UnitType == UnitType.UT_Angle;
#endif
        }

        /// <summary>
        /// 转换为Double类型
        /// </summary>
        private static double ConvertToDouble(object value)
        {
            if (value is double d) return d;
            if (value is int i) return (double)i;
            if (value is long l) return (double)l;
            if (value is float f) return (double)f;
            if (value is string s && double.TryParse(s, out double result)) return result;

            throw new ArgumentException($"无法将 {value} 转换为 Double 类型");
        }

        /// <summary>
        /// 转换为Int类型
        /// </summary>
        private static int ConvertToInt(object value)
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)Math.Round(d);
            if (value is string s && int.TryParse(s, out int result)) return result;

            throw new ArgumentException($"无法将 {value} 转换为 Int 类型");
        }

        /// <summary>
        /// 转换为ElementId类型
        /// </summary>
        private static ElementId ConvertToElementId(object value)
        {
            if (value is ElementId elemId) return elemId;
            if (value is int i) return new ElementId(i);
            if (value is long l) return new ElementId((int)l);
            if (value is string s && int.TryParse(s, out int result)) return new ElementId(result);

            throw new ArgumentException($"无法将 {value} 转换为 ElementId 类型");
        }
    }
}