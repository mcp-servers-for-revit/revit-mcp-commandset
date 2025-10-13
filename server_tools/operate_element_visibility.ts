import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerOperateElementVisibilityTool(server: McpServer) {
  server.tool(
    "operate_element_visibility",
    `可见性控制操作：隐藏、临时隐藏、隔离显示、取消隐藏、重置视图。适用于视图清理、施工阶段管理、专业系统聚焦。AI Token使用减少50%+。

⚠️ 操作限制：
- Isolate（隔离显示）：仅适用于实例元素（FamilyInstance、Wall、Floor等）
- 类型元素（FamilySymbol、WallType等，identity.category=null）不支持隔离操作
- 建议：确保传入的 elementIds 是实例的 ID
- 识别实例：通过 ai_element_filter 返回的 identity.category 字段判断（有值=实例，null=类型/族）

🔄 失败回退策略：
- 若误传类型元素导致 Isolate 操作超时或失败
- 请先通过 ai_element_filter 使用 filterTypeId 获取该类型的实例 ElementId
- 然后对实例执行 Isolate 操作

支持的操作：
- Hide：持久隐藏，直到明确显示
- TempHide：临时隐藏，切换视图后恢复
- Isolate：隔离显示，仅显示选中元素（仅适用于实例）
- Unhide：取消隐藏，恢复元素可见性
- ResetIsolate：重置隔离状态，恢复正常视图`,
    {
      data: z
        .object({
          elementIds: z
            .array(z
              .number()
              .describe("Revit元素ID")
            )
            .describe("目标元素ID数组"),
          visibilityAction: z
            .string()
            .describe(`可见性操作类型。

支持的操作：
- Hide（持久隐藏，直到明确显示）：适用于所有元素
- TempHide（临时隐藏，切换视图后恢复）：适用于所有元素
- Isolate（隔离显示，仅显示选中元素）：仅适用于实例元素，类型元素会超时或失败
- Unhide（取消隐藏，恢复元素可见性）：适用于所有元素
- ResetIsolate（重置隔离状态，恢复正常视图）：适用于当前视图

⚠️ 如果对类型元素（identity.category=null）执行 Isolate 操作会超时
🔄 解决方法：
1. 使用 ai_element_filter 的 filterTypeId 参数获取该类型的实例
   示例：{"data": {"filterTypeId": 337201, "includeInstances": true, "fields": ["identity"]}}
2. 对返回的实例 ElementId 执行 Isolate 操作`)
        })
        .describe("可见性控制参数配置。专注当前视图的元素可见性管理"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "operate_element_visibility",
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
              text: `可见性操作失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}