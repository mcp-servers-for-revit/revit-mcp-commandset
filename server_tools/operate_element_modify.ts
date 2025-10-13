import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerOperateElementModifyTool(server: McpServer) {
  server.tool(
    "operate_element_modify",
    "参数修改与删除操作：修改参数值、删除元素。适用于属性批量更新、构件清理、数据修正。AI Token使用减少45%+，支持自动单位转换。",
    {
      data: z
        .object({
          elementIds: z
            .array(z
              .number()
              .describe("Revit元素ID")
            )
            .describe("目标元素ID数组"),
          modifyAction: z
            .string()
            .describe("修改操作类型。支持的操作：SetParameter（修改参数值）、Delete（删除元素）"),

          // SetParameter 专用参数
          parameterName: z
            .string()
            .optional()
            .describe("参数名称，仅SetParameter操作需要。支持实例参数和类型参数，使用Revit UI中显示的参数名（注意语言版本）"),
          parameterValue: z
            .union([z.string(), z.number(), z.boolean()])
            .optional()
            .describe("参数值，仅SetParameter操作需要。支持文本、数值、布尔值。长度单位自动转换（毫米→英尺），角度自动转换（度→弧度）"),
          parameterType: z
            .string()
            .optional()
            .describe("参数类型提示，可选。支持：Text、Integer、Number、YesNo、Length、Angle等。用于辅助类型转换和验证"),
          isBuiltIn: z
            .boolean()
            .default(false)
            .describe("是否为内置参数，默认false。如果为true，parameterName应使用BuiltInParameter枚举名称")
        })
        .describe("参数修改和删除配置。SetParameter支持自动单位转换，Delete为永久性操作"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "operate_element_modify",
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
              text: `参数修改操作失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}