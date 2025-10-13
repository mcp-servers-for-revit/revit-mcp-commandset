import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetElementCreationSuggestionTool(server: McpServer) {
  server.tool(
    "get_element_creation_suggestion",
    "获取统一的元素创建参数要求和指导建议。为AI提供族实例和系统族元素的创建参数格式、必需参数、可选参数及示例。支持根据ElementId自动分析类型并提供相应的创建建议，帮助AI理解如何正确使用create_element命令。",
    {
      data: z.object({
        elementClass: z
          .enum(["Family", "System"])
          .optional()
          .describe("元素类别。可选，指定要获取建议的元素类型。Family=族实例，System=系统族元素。"),

        elementId: z
          .number()
          .optional()
          .describe("用于查询特定元素类型的ElementId。当指定时，系统会分析该元素并提供对应的创建参数建议。"),

        elementType: z
          .string()
          .optional()
          .describe("系统族类型名。支持值：'wall'（墙体）、'floor'（楼板）等。用于获取特定系统族类型的创建建议。"),

        returnAll: z
          .boolean()
          .default(false)
          .describe("是否返回所有可用的创建建议。当为true时，忽略其他过滤条件，返回完整的参数格式指南。")
      }).optional().describe("查询参数。如果不提供任何参数，返回通用的创建参数建议。")
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_element_creation_suggestion", args);
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
              text: `获取创建建议失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}