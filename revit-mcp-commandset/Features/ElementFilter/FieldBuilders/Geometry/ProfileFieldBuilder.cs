using Autodesk.Revit.DB;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Features.ElementFilter.FieldBuilders.Geometry
{
    /// <summary>
    /// 轮廓字段构建器
    /// 构建 geometry.profile 字段：返回平面构件的精确轮廓
    /// </summary>
    public class ProfileFieldBuilder : IFieldBuilder
    {
        public string FieldName => "geometry.profile";

        public bool CanBuild(Element element)
        {
            // 支持楼板、天花板、屋顶等平面构件
            var category = element?.Category?.Id.IntegerValue;
            return category == (int)BuiltInCategory.OST_Floors ||
                   category == (int)BuiltInCategory.OST_Ceilings ||
                   category == (int)BuiltInCategory.OST_Roofs;
        }

        public void Build(FieldContext context)
        {
            try
            {
                // 使用扩展方法获取轮廓
                var profiles = context.Element.GetElementProfile();

                if (!context.Result.ContainsKey("geometry"))
                    context.Result["geometry"] = new Dictionary<string, object>();

                var geoDict = context.Result["geometry"] as Dictionary<string, object>;

                if (profiles != null && profiles.Count > 0)
                {
                    // 直接输出List<JZFace>
                    geoDict["profile"] = profiles;

                    // 检查是否有非线性边的警告
                    foreach (var face in profiles)
                    {
                        if (face == null)
                        {
                            context.AddWarning("部分轮廓包含不支持的曲线类型（如圆弧），已跳过");
                            break;
                        }
                    }
                }
                else
                {
                    context.AddWarning("未找到可用轮廓");
                }
            }
            catch (Exception ex)
            {
                context.AddWarning($"轮廓获取失败: {ex.Message}");
            }
        }
    }
}