import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerOperateElementTransformTool(server: McpServer) {
  server.tool(
    "operate_element_transform",
    "几何变换操作：旋转、镜像、翻转、移动、复制。适用于位置调整、布局优化、对称设计。AI Token使用减少55%+。P1阶段Move操作仅支持directTransform策略。",
    {
      data: z
        .object({
          elementIds: z
            .array(z
              .number()
              .describe("Revit元素ID")
            )
            .describe("目标元素ID数组"),
          transformAction: z
            .string()
            .describe("变换操作类型。支持的操作：Rotate（绕轴旋转）、Mirror（沿平面镜像）、Flip（翻转族实例）、Move（移动元素，P1仅directTransform）、Copy（复制元素到指定位置）"),

          // Rotate 专用参数
          rotateAxis: z
            .object({
              p0: z.object({
                x: z.number().describe("起点X坐标（毫米）"),
                y: z.number().describe("起点Y坐标（毫米）"),
                z: z.number().describe("起点Z坐标（毫米）")
              }).describe("旋转轴起点"),
              p1: z.object({
                x: z.number().describe("终点X坐标（毫米）"),
                y: z.number().describe("终点Y坐标（毫米）"),
                z: z.number().describe("终点Z坐标（毫米）")
              }).describe("旋转轴终点")
            })
            .optional()
            .describe("旋转轴线定义，仅Rotate操作使用。可选参数，如未提供将智能获取元素位置作为Z轴旋转"),
          rotateAngle: z
            .number()
            .optional()
            .describe("旋转角度（度数），仅Rotate操作需要。正值为逆时针旋转"),

          // Mirror 专用参数
          mirrorPlane: z
            .object({
              origin: z.object({
                x: z.number().describe("原点X坐标（毫米）"),
                y: z.number().describe("原点Y坐标（毫米）"),
                z: z.number().describe("原点Z坐标（毫米）")
              }).describe("平面原点").optional(),
              normal: z.object({
                x: z.number().describe("法向量X分量（归一化）"),
                y: z.number().describe("法向量Y分量（归一化）"),
                z: z.number().describe("法向量Z分量（归一化）")
              }).describe("平面法向量（需要归一化）")
            })
            .optional()
            .describe("镜像平面定义，仅Mirror操作使用。可选参数，如未提供将智能获取元素位置作为YZ平面镜像"),

          // Flip 专用参数
          flipDirection: z
            .string()
            .optional()
            .describe("翻转方向，仅Flip操作需要。支持值：Hand（左右翻转）、Facing（前后翻转）。仅适用于族实例"),

          // Move 和 Copy 专用参数
          moveVector: z
            .object({
              x: z.number().describe("X方向位移（毫米）"),
              y: z.number().describe("Y方向位移（毫米）"),
              z: z.number().describe("Z方向位移（毫米）")
            })
            .optional()
            .describe("移动向量定义，Move和Copy操作需要。所有坐标单位为毫米"),
          moveStrategy: z
            .string()
            .default("directTransform")
            .describe("移动策略。P1阶段仅支持directTransform（直接变换），适用于独立族实例、墙体、楼板。recreate策略将在P2实现")
        })
        .describe("几何变换参数配置。支持旋转、镜像、翻转、移动、复制等操作"),
    },
    async (args, extra) => {
      const params = args;

      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand(
            "operate_element_transform",
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
              text: `几何变换操作失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}