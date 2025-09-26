using Autodesk.Revit.DB;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Features.ElementFilter.FieldBuilders.Geometry
{
    /// <summary>
    /// 统一的位置字段构建器
    /// 构建 geometry.location 字段：根据元素Location类型自动返回point或line
    /// </summary>
    public class LocationFieldBuilder : IFieldBuilder
    {
        public string FieldName => "geometry.location";

        public bool CanBuild(Element element)
        {
            return element?.Location != null;
        }

        public void Build(FieldContext context)
        {
            try
            {
                var locationInfo = GeometryUtils.FromLocation(context.Element.Location, context);

                if (locationInfo == null) return;

                // 确保geometry节点存在
                if (!context.Result.ContainsKey("geometry"))
                    context.Result["geometry"] = new Dictionary<string, object>();

                var geoDict = context.Result["geometry"] as Dictionary<string, object>;

                // LocationInfo会自动序列化为正确的结构
                geoDict["location"] = locationInfo;
            }
            catch (Exception ex)
            {
                context.AddWarning($"获取位置信息失败: {ex.Message}");
            }
        }
    }
}