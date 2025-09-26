using Autodesk.Revit.DB;

namespace RevitMCPCommandSet.Features.ElementFilter.FieldBuilders.Core
{
    /// <summary>
    /// 类型信息字段构建器
    /// 构建统一的 type 字段：typeId, typeName, familyId（族实例）, familyName（族实例）
    /// </summary>
    public class TypeInfoFieldBuilder : IFieldBuilder
    {
        public string FieldName => "type";

        public bool CanBuild(Element element)
        {
            // 所有元素都有类型信息，确保始终返回 type 节点
            return element != null;
        }

        public void Build(FieldContext context)
        {
            var element = context.Element;
            var typeId = element.GetTypeId();

            // 基础类型信息
            if (typeId == ElementId.InvalidElementId)
            {
                // 对于没有类型的元素（如Project Info等），返回默认值
                context.SetNodeValue("type", "typeId", -1);
                context.SetNodeValue("type", "typeName", null);
            }
            else
            {
                // 使用缓存的 TypeElement
                var typeElement = context.TypeElement;

                if (typeElement != null)
                {
                    context.SetNodeValue("type", "typeId", typeId.IntegerValue);
                    context.SetNodeValue("type", "typeName", typeElement.Name);
                }
                else
                {
                    // 类型元素不存在的情况
                    context.SetNodeValue("type", "typeId", typeId.IntegerValue);
                    context.SetNodeValue("type", "typeName", null);
                }
            }

            // 族实例的额外信息（统一到type节点下）
            if (element is FamilyInstance)
            {
                var family = context.Family;

                if (family != null)
                {
                    context.SetNodeValue("type", "familyId", family.Id.IntegerValue);
                    context.SetNodeValue("type", "familyName", family.Name);
                }
                else
                {
                    // 族信息不可用的情况
                    context.SetNodeValue("type", "familyId", -1);
                    context.SetNodeValue("type", "familyName", null);
                }
            }
            // 注意：非族实例不会有familyId和familyName字段
        }
    }
}