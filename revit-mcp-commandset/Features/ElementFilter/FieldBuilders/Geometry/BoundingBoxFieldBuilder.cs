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

                // 使用GeometryUtils转换为BoundingBoxInfo并写入geometry节点
                var boundingBoxInfo = GeometryUtils.FromBoundingBoxXYZ(boundingBox);
                if (boundingBoxInfo != null)
                {
                    context.SetNodeValue("geometry", "boundingBox", boundingBoxInfo);
                }
            }
            catch
            {
                // 静默失败
            }
        }
    }
}