using Autodesk.Revit.DB;
using RevitMCPCommandSet.Utils;

namespace RevitMCPCommandSet.Features.ElementFilter.FieldBuilders.Geometry
{
    /// <summary>
    /// 边界框字段构建器
    /// 构建 geometry.boundingBox 字段：返回BoundingBoxInfo结构
    /// </summary>
    public class BoundingBoxFieldBuilder : IFieldBuilder
    {
        public string FieldName => "geometry.boundingBox";

        public bool CanBuild(Element element)
        {
            return element != null;
        }

        public void Build(FieldContext context)
        {
            try
            {
                // 使用缓存的边界框
                var boundingBox = context.BoundingBox;
                if (boundingBox == null || !boundingBox.Enabled)
                {
                    return; // 无法获取边界框，静默失败
                }

                // 确保geometry节点存在
                if (!context.Result.ContainsKey("geometry"))
                    context.Result["geometry"] = new Dictionary<string, object>();

                var geoDict = context.Result["geometry"] as Dictionary<string, object>;

                // 使用GeometryUtils转换为BoundingBoxInfo
                var boundingBoxInfo = GeometryUtils.FromBoundingBoxXYZ(boundingBox);
                if (boundingBoxInfo != null)
                {
                    geoDict["boundingBox"] = boundingBoxInfo;
                }
            }
            catch
            {
                // 静默失败
            }
        }
    }
}