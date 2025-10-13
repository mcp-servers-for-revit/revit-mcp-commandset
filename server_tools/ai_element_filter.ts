import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerAIElementFilterTool(server: McpServer) {
  server.tool(
    "ai_element_filter",
    `智能Revit元素查询工具，采用结构化节点数据架构。

📊 元素类型识别规则（重要）：
- 族实例 (FamilyInstance)：identity.category 有值（如"电气设备"、"门"、"窗"）
- 类型 (FamilySymbol)：identity.category 为 null，且当前查询为 includeTypes=true
- 族 (Family)：identity.category 为 null（较少直接返回，通常需要专门查询）
- 判别方法：
  * 当 includeTypes=true + includeInstances=false 时，返回的 category=null 元素为类型
  * 当 includeInstances=true + includeTypes=false 时，返回的 category 有值元素为实例
  * 若返回混合结果，需通过 identity.category 字段逐一判断
- 字段映射：
  * 实例的 identity.name → 通常是类型名称（如"标准"）
  * 实例的 type.familyName → 族名称（如"JZ-I-蒸烤一体机"）
  * 实例的 type.familyId → 族ID
  * 类型的 identity.name → 类型名称或族名称（如"JZ-I-蒸烤一体机"）
  * ⚠️ 类型查询不返回 type.familyName/familyId，族信息需通过后续实例查询获得

🔍 推荐查询流程：
流程A（双回合 - 类别未知时推荐）：
  步骤1：includeTypes=true + filterNameKeyword + maxElements → 获取类型 ElementId
  步骤2：filterTypeId + includeInstances=true → 获取该类型的所有实例
  优点：结果清晰、性能可控

流程B（单回合 - 类别已知时可用）：
  filterCategory + filterNameKeyword + includeInstances=true + maxElements → 直接获取实例
  ⚠️ 注意：必须使用 fieldPresets:['typeAnalysis'] 以获得足够的判别信息
  ⚠️ 若类别判断有误导致返回混合集合，仍可通过上述识别规则中的 identity.category 字段识别实例

功能特性：
1) 结构化数据节点（identity、type[统一包含族信息]、level、geometry、parameters）
2) 统一的geometry.location字段（自动检测点/线）
3) geometry.profile精确楼板/天花轮廓
4) fields + fieldPresets双轨系统
   - fieldPresets 提供预定义组合（如 'typeAnalysis' 返回 identity + type）
   - fields 可补充额外字段（如 geometry.location）
   - 示例：fieldPresets:['typeAnalysis'], fields:['geometry.location']
5) 直接ElementId查询

📋 典型使用场景：

场景1：通过族名称查找族实例（推荐双回合流程）
步骤1 - 查找类型：
{
  "data": {
    "filterNameKeyword": "蒸烤一体机",
    "includeTypes": true,
    "includeInstances": false,
    "fieldPresets": ["typeAnalysis"],
    "maxElements": 50
  }
}
→ 返回类型元素，从 response 中识别 identity.category=null 的类型
→ 通过 identity.name 包含关键字"蒸烤一体机"确认匹配，获取其 elementId（如 337201）
→ ⚠️ 注意：类型查询不返回 type.familyName，需根据 identity.name 判断

步骤2 - 获取实例：
{
  "data": {
    "filterTypeId": 337201,
    "includeInstances": true,
    "includeTypes": false,
    "fields": ["identity", "type", "geometry.location"],
    "maxElements": 50
  }
}
→ 返回该类型的所有实例，通过 identity.category 有值（如"电气设备"）确认为实例
→ ✅ 此步骤返回完整的 type.familyName/familyId 族信息

场景2：已知类别，直接查找实例（单回合流程）
{
  "data": {
    "filterCategory": "OST_ElectricalEquipment",
    "filterNameKeyword": "蒸烤一体机",
    "includeInstances": true,
    "includeTypes": false,
    "fieldPresets": ["typeAnalysis"],
    "maxElements": 50
  }
}
→ 直接返回电气设备类别下名称包含"蒸烤一体机"的实例
→ 通过 identity.category="电气设备" 和 type.familyName 识别
→ ⚠️ 若类别选择错误可能返回空结果或混合结果，此时需通过 identity.category 字段逐一识别实例

场景3：通过 ElementId 直接查询
{
  "data": {
    "elementIds": [344176],
    "fields": ["identity", "type", "geometry"],
    "maxElements": 50
  }
}
→ 直接返回指定元素的详细信息

场景4：关键字检索模式（新增支持）
{
  "data": {
    "filterNameKeyword": "蒸烤一体机",
    "includeInstances": true,
    "includeTypes": false,
    "maxElements": 50
  }
}
→ ✅ 支持仅使用关键字进行检索，无需指定类别或类型约束
→ ⚠️ 关键字模式 maxElements 上限为 100（建议 20-50），避免性能问题
→ 💡 建议配合 filterCategory 或 filterElementType 以提升性能

重要提示：
- 参数名必须匹配Revit UI语言。先使用'get_revit_status'检查ApplicationLanguage
- 中文环境使用中文参数名（如'高度'、'无连接高度'），英文环境使用英文（如'Height'）
- 查询内置参数时，设置includeBuiltIn=true并使用BuiltInParameter枚举名
- 建议默认使用 fieldPresets:['typeAnalysis'] 以包含足够的类型信息用于元素识别
- 大型项目中务必配置 maxElements 参数（建议默认 50）以保护性能`,
    {
      data: z.object({
        filterCategory: z
          .string()
          .optional()
          .describe("Revit内置元素类别枚举，用于过滤和识别特定元素类型（如OST_Walls、OST_Floors、OST_GenericModel）。注意：家具元素可能被归类为OST_Furniture或OST_GenericModel类别，需要灵活选择"),
        filterElementType: z
          .string()
          .optional()
          .describe("Revit元素类型名称，用于按类或类型过滤特定元素（如'Wall'、'Floor'、'Autodesk.Revit.DB.Wall'）。获取或设置要过滤的Revit元素类型名称"),
        filterTypeId: z
          .number()
          .optional()
          .describe("统一的类型ElementId过滤器。对于族实例，匹配FamilySymbol的ElementId；对于系统族实例（如墙体、楼板），匹配WallType/FloorType的ElementId；对于类型元素本身，匹配元素自身的ElementId。如果不需要类型过滤，使用'-1'"),
        filterNameKeyword: z
          .string()
          .optional()
          .describe(`名称关键字过滤条件。检查元素名称、类型名称、族名称是否包含此关键字（不区分大小写）。

✅ 支持单独使用（关键字检索模式）：
- 无需指定 filterCategory、filterElementType 或 filterTypeId
- 必须配合 maxElements 参数（上限 100，建议 20-50）
- 适用于快速搜索特定元素，如'单扇窗'、'蒸烤一体机'等

⚠️ 性能优化建议：
- 推荐搭配 filterCategory 或 filterElementType 以提升性能
- 若类别未知，可配合 includeTypes=true/false 进行双回合查询：
  步骤1：includeTypes=true + includeInstances=false + filterNameKeyword + maxElements:50 → 查找类型
  步骤2：filterTypeId + includeInstances=true → 获取实例

设置为null或空字符串可禁用名称过滤`),
        includeTypes: z
          .boolean()
          .default(false)
          .describe(`是否在选择结果中包含元素类型（如墙类型、门类型等）。

推荐用法：
- 双回合流程第1步：includeTypes=true + includeInstances=false → 仅返回类型
- 与 includeInstances 配合：
  * 两者均为 false：无结果
  * 两者均为 true：返回类型和实例的混合结果（需通过 identity.category 判别）
  * 仅 includeTypes=true：仅返回类型（identity.category=null）
  * 仅 includeInstances=true：仅返回实例（identity.category 有值）

⚠️ 注意：类型查询不返回 type.familyName/familyId，仅有 identity.* 和基本类型信息`),
        includeInstances: z
          .boolean()
          .default(true)
          .describe(`是否在选择结果中包含元素实例（如已放置的墙体、门等）。

推荐用法：
- 双回合流程第2步：includeInstances=true + includeTypes=false → 仅返回实例
- 单回合流程：includeInstances=true + includeTypes=false + filterCategory → 直接获取实例
- 与 includeTypes 配合：
  * 两者均为 false：无结果
  * 两者均为 true：返回类型和实例的混合结果（需通过 identity.category 判别）
  * 仅 includeInstances=true：仅返回实例（identity.category 有值）
  * 仅 includeTypes=true：仅返回类型（identity.category=null）

✅ 注意：实例查询会返回完整的 type.familyName/familyId 族信息`),
        filterVisibleInCurrentView: z
          .boolean()
          .optional()
          .describe("是否仅返回在当前视图中可见的元素。设置为true时，仅返回当前视图中可见的元素。注意：此过滤器仅适用于元素实例，不适用于类型元素"),
        boundingBoxMin: z
          .object({
            p0: z.object({
              x: z.number().describe("起点X坐标"),
              y: z.number().describe("起点Y坐标"),
              z: z.number().describe("起点Z坐标"),
            }),
            p1: z.object({
              x: z.number().describe("终点X坐标"),
              y: z.number().describe("终点Y坐标"),
              z: z.number().describe("终点Z坐标"),
            }),
          })
          .optional()
          .describe("空间包围盒过滤的最小点坐标（单位：毫米）。与boundingBoxMax一起设置时，仅返回与此包围盒相交的元素。设置为null可禁用此过滤器"),
        boundingBoxMax: z
          .object({
            p0: z.object({
              x: z.number().describe("起点X坐标"),
              y: z.number().describe("起点Y坐标"),
              z: z.number().describe("起点Z坐标"),
            }),
            p1: z.object({
              x: z.number().describe("终点X坐标"),
              y: z.number().describe("终点Y坐标"),
              z: z.number().describe("终点Z坐标"),
            }),
          })
          .optional()
          .describe("空间包围盒过滤的最大点坐标（单位：毫米）。与boundingBoxMin一起设置时，仅返回与此包围盒相交的元素。设置为null可禁用此过滤器"),
          maxElements: z
          .number()
          .optional()
          .describe(`返回元素数量限制，默认 50。关键字检索模式（仅使用 filterNameKeyword）下硬性上限 100，超出报错。建议配合 filterCategory 提升性能`),
        elementIds: z
          .array(z.number())
          .optional()
          .describe("直接查询的元素ID列表。单独提供时，直接检索这些元素，绕过其他过滤器。与其他过滤器组合使用时，返回两种条件的交集。适用于：1) 按ID检索已知元素，2) 验证元素存在性，3) 获取特定元素的详细信息"),
        fields: z
          .array(z.string())
          .optional()
          .describe("原子字段列表，用于精确数据控制。可用字段：'name'、'category'、'builtInCategory'、'identity'、'type'（族实例包含familyId/familyName）、'level'、'geometry.location'、'geometry.profile'、'geometry.boundingBox'、'geometry.thickness'、'geometry.height'、'geometry.width'、'geometry.area'。示例：['name', 'geometry.location', 'type']。可与fieldPresets组合使用"),
        fieldPresets: z
          .array(z.string())
          .optional()
          .describe(`常见场景的预定义字段组合。

可用预设：
- 'typeAnalysis'（推荐）：identity + type → 包含完整的类型和族信息（type.familyId、type.familyName），用于元素识别
- 'spatialAnalysis'：identity + geometry.location + geometry.boundingBox → 空间查询
- 'detailView'：identity + type + level → 详细视图
- 'familyAnalysis'：identity + type[包含族信息] → 族分析
- 'floorAnalysis'：identity + geometry.profile + geometry.boundingBox → 楼板/天花分析
- 'listDisplay'：仅identity → 仅适用于简单列表展示

⚠️ 重要：
- 查询族实例时必须使用 ['typeAnalysis'] 以获得足够的判别信息（identity.category、type.familyName 等）
- fieldPresets 和 fields 可以组合使用，例如：
  * fieldPresets:['typeAnalysis'], fields:['geometry.location'] → 获取类型信息 + 位置信息
  * fieldPresets:['spatialAnalysis', 'typeAnalysis'] → 同时获取空间和类型信息

示例：['typeAnalysis'], ['typeAnalysis', 'spatialAnalysis']`),
        parameters: z
          .object({
            includeInstance: z.boolean().default(true).describe("包含实例参数"),
            includeType: z.boolean().default(false).describe("包含类型参数"),
            includeBuiltIn: z.boolean().default(false).describe("包含内置参数"),
            names: z.array(z.string()).optional().describe("要检索的特定参数名称"),
            singleName: z.string().optional().describe("单个参数名称（names数组的替代方式）"),
            flatten: z.boolean().default(true).describe("将参数展平到根级别（true）或嵌套在'parameters'键下（false）")
          })
          .optional()
          .describe("参数检索配置。控制要包含哪些参数以及如何格式化它们。使用singleName检索单个参数，使用names检索多个参数。设置flatten=false可将参数分组到'parameters'键下。注意：参数名称依赖于语言。对于中文Revit，使用中文名称如'无连接高度'而不是'Height'。查询内置参数时考虑设置includeBuiltIn=true"),
      })
        .describe("Revit元素过滤工具的配置参数。这些设置决定根据各种过滤条件从Revit项目中选择哪些元素。可以组合多个过滤器以实现精确的元素选择。所有空间坐标应以毫米为单位提供"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "ai_element_filter",
            params
          );
        });

        return {
          content: [
            {
              type: "text",
              text: JSON.stringify(response, null, 2),
            },
          ],
        };
      } catch (error) {
        return {
          content: [
            {
              type: "text",
              text: `获取元素信息失败: ${error instanceof Error ? error.message : String(error)
                }`,
            },
          ],
        };
      }
    }
  );
}
