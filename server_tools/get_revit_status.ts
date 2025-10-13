import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetRevitStatusTool(server: McpServer) {
  server.tool(
    "get_revit_status",
    "获取当前Revit应用程序的状态信息，包括版本信息、当前打开的项目、活动视图、文档状态等详细信息。此工具不需要任何输入参数。",
    {
      // 此工具不需要任何参数
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("get_revit_status", {});
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
              text: `获取Revit状态失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}