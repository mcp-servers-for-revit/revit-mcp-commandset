import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateElementTool(server: McpServer) {
  server.tool(
    "create_element",
    "统一的Revit元素创建命令，支持族实例和系统族元素的创建。族实例支持8种放置类型（OneLevelBased、WorkPlaneBased、TwoLevelsBased、CurveBased、ViewBased等），系统族支持墙体、楼板等。自动处理类型检测、标高查找、宿主搜索和参数验证。所有坐标单位为毫米(mm)。",
    {
      data: z.object({
        elementClass: z
          .enum(["Family", "System"])
          .optional()
          .describe("元素类别。可选，系统会根据typeId自动检测。Family=族实例，System=系统族元素"),

        typeId: z
          .number()
          .describe("族类型或系统族类型的ElementId。必须是项目中已加载的有效类型。"),

        levelId: z
          .number()
          .optional()
          .describe("标高ElementId。可选，如果不指定且autoFindLevel为true，系统会自动查找最近标高。"),

        autoFindLevel: z
          .boolean()
          .default(true)
          .describe("是否自动查找最近标高。当levelId未指定时生效。"),

        familyOptions: z
          .object({
            locationPoint: z
              .object({
                x: z.number().describe("X坐标（毫米）"),
                y: z.number().describe("Y坐标（毫米）"),
                z: z.number().describe("Z坐标（毫米）")
              })
              .optional()
              .describe("点定位参数。用于OneLevelBased、OneLevelBasedHosted、TwoLevelsBased、WorkPlaneBased、ViewBased族类型。"),

            locationLine: z
              .object({
                p0: z.object({
                  x: z.number().describe("起点X坐标（毫米）"),
                  y: z.number().describe("起点Y坐标（毫米）"),
                  z: z.number().describe("起点Z坐标（毫米）")
                }),
                p1: z.object({
                  x: z.number().describe("终点X坐标（毫米）"),
                  y: z.number().describe("终点Y坐标（毫米）"),
                  z: z.number().describe("终点Z坐标（毫米）")
                })
              })
              .optional()
              .describe("线定位参数。用于CurveBased、CurveBasedDetail、CurveDrivenStructural族类型。"),

            topLevelId: z
              .number()
              .default(-1)
              .describe("顶部标高ElementId。用于TwoLevelsBased族类型（如结构柱）。"),

            baseOffset: z
              .number()
              .default(0)
              .describe("底部偏移（毫米）。正值向上，负值向下。"),

            topOffset: z
              .number()
              .default(0)
              .describe("顶部偏移（毫米）。用于TwoLevelsBased族类型。"),

            viewId: z
              .number()
              .default(-1)
              .describe("视图ElementId。用于ViewBased和CurveBasedDetail族类型（如注释、详图构件）。"),

            hostElementId: z
              .number()
              .default(-1)
              .describe("宿主元素ElementId。用于OneLevelBasedHosted族类型。如果不指定且autoFindHost为true，系统会自动搜索合适宿主。"),

            hostCategories: z
              .array(z.string())
              .optional()
              .describe("宿主类别数组（BuiltInCategory名称）。常用值：'OST_Walls'、'OST_Floors'、'OST_Ceilings'、'OST_Roofs'、'OST_GenericModel'。"),

            faceDirection: z
              .object({
                x: z.number().describe("面方向向量X分量（归一化）"),
                y: z.number().describe("面方向向量Y分量（归一化）"),
                z: z.number().describe("面方向向量Z分量（归一化）")
              })
              .optional()
              .describe("面方向向量（归一化）。用于WorkPlaneBased族类型，通常指向工作平面的法向。"),

            handDirection: z
              .object({
                x: z.number().describe("手方向向量X分量（归一化）"),
                y: z.number().describe("手方向向量Y分量（归一化）"),
                z: z.number().describe("手方向向量Z分量（归一化）")
              })
              .optional()
              .describe("手方向向量（归一化）。用于WorkPlaneBased族类型，定义族坐标系的X轴方向。"),

            autoFindHost: z
              .boolean()
              .default(true)
              .describe("是否自动查找宿主元素。当hostElementId未指定时生效。"),

            searchRadius: z
              .number()
              .default(1000)
              .describe("搜索半径（毫米）。用于自动查找宿主元素。较大值增加搜索范围但可能影响性能。")
          })
          .optional()
          .describe("族创建特有参数。仅在elementClass为'Family'或自动检测为族时使用。"),

        systemOptions: z
          .object({
            elementType: z
              .string()
              .describe("系统族类型。支持值：'wall'（墙体）、'floor'（楼板）。"),

            isStructural: z
              .boolean()
              .default(false)
              .describe("是否为结构构件。"),

            wallParameters: z
              .object({
                line: z.object({
                  p0: z.object({
                    x: z.number().describe("起点X坐标（毫米）"),
                    y: z.number().describe("起点Y坐标（毫米）"),
                    z: z.number().describe("起点Z坐标（毫米）")
                  }),
                  p1: z.object({
                    x: z.number().describe("终点X坐标（毫米）"),
                    y: z.number().describe("终点Y坐标（毫米）"),
                    z: z.number().describe("终点Z坐标（毫米）")
                  })
                }).describe("墙体路径线"),

                height: z
                  .number()
                  .describe("墙体高度（毫米）。"),

                baseOffset: z
                  .number()
                  .default(0)
                  .describe("墙体底部偏移（毫米）。正值向上，负值向下。"),

                thickness: z
                  .number()
                  .optional()
                  .describe("墙体厚度（毫米）。可选，如果不指定则使用类型默认厚度。")
              })
              .optional()
              .describe("墙体专用参数。仅在elementType为'wall'时使用。"),

            floorParameters: z
              .object({
                boundary: z
                  .array(z.object({
                    x: z.number().describe("点X坐标（毫米）"),
                    y: z.number().describe("点Y坐标（毫米）"),
                    z: z.number().describe("点Z坐标（毫米）")
                  }))
                  .min(3)
                  .describe("楼板边界点数组。至少3个点形成封闭区域。"),

                topOffset: z
                  .number()
                  .default(0)
                  .describe("楼板顶部偏移（毫米）。正值向上，负值向下。"),

                thickness: z
                  .number()
                  .optional()
                  .describe("楼板厚度（毫米）。可选，如果不指定则使用类型默认厚度。")
              })
              .optional()
              .describe("楼板专用参数。仅在elementType为'floor'时使用。")
          })
          .optional()
          .describe("系统族创建特有参数。仅在elementClass为'System'或自动检测为系统族时使用。")
      })
    },
    async (args, extra) => {
      try {
        const response = await withRevitConnection(async (revitClient) => {
          return await revitClient.sendCommand("create_element", args);
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
              text: `元素创建失败: ${error instanceof Error ? error.message : String(error)}`,
            },
          ],
        };
      }
    }
  );
}