import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerOperateElementVisualTool(server: McpServer) {
  server.tool(
    "operate_element_visual",
    `纯视觉效果操作：选择、高亮、着色、透明度、3D聚焦。适用于冲突标记、专业分类、视图展示。AI Token使用减少60%+，最轻量的元素操作命令。

⚠️ 操作限制：
- SelectionBox：仅适用于有几何形状的实例元素（FamilyInstance、Wall、Floor等）
- 类型元素（FamilySymbol、WallType等，identity.category=null）不支持 SelectionBox 操作
- 建议：对实例使用 SelectionBox，对类型使用 Select
- 识别实例：通过 ai_element_filter 返回的 identity.category 字段判断（有值=实例，null=类型/族）

🔄 失败回退策略：
- 若误传类型元素导致 SelectionBox 失败（提示"无法创建边界框"）
- 请先通过 ai_element_filter 使用 filterTypeId 获取该类型的实例 ElementId
- 然后对实例执行 SelectionBox 或 Isolate 操作

支持的操作：
- Select：选择元素（适用于所有元素）
- SelectionBox：3D选择框聚焦（仅适用于实例元素）
- Highlight：快捷红色高亮（适用于所有可见元素）
- SetColor：自定义颜色标记（适用于所有可见元素）
- SetTransparency：透明度调整（适用于所有可见元素）`,
    {
      data: z
        .object({
          elementIds: z
            .array(z
              .number()
              .describe("Revit元素ID")
            )
            .describe("目标元素ID数组"),
          visualAction: z
            .string()
            .describe(`视觉操作类型。

支持的操作：
- Select（选择元素）：适用于所有元素
- SelectionBox（3D选择框聚焦）：仅适用于实例元素，类型元素会失败
- Highlight（快捷红色高亮）：适用于所有可见元素
- SetColor（自定义颜色）：适用于所有可见元素
- SetTransparency（透明度调整）：适用于所有可见元素

⚠️ 如果对类型元素（identity.category=null）执行 SelectionBox 会失败（提示"无法创建边界框"）
🔄 解决方法：
1. 使用 ai_element_filter 的 filterTypeId 参数获取该类型的实例
   示例：{"data": {"filterTypeId": 337201, "includeInstances": true, "fields": ["identity"]}}
2. 对返回的实例 ElementId 执行 SelectionBox 操作`),
          colorValue: z
            .array(z.number())
            .default([255, 0, 0])
            .describe("RGB颜色值数组，用于SetColor操作。格式：[R, G, B]，取值范围0-255。默认红色[255,0,0]"),
          transparencyValue: z
            .number()
            .default(50)
            .describe("透明度值（0-100），用于SetTransparency操作。0=完全不透明，100=完全透明。默认50")
        })
        .describe("视觉操作参数配置。专注纯视觉效果，不改变模型几何或属性"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "operate_element_visual",
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
              text: `视觉操作失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}