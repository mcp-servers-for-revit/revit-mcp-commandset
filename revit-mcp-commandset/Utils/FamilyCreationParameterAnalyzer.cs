using Autodesk.Revit.DB;
using RevitMCPCommandSet.Models.Common;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 族创建参数分析器
    /// </summary>
    public class FamilyCreationParameterAnalyzer
    {
        /// <summary>
        /// 分析族类型的创建参数需求
        /// </summary>
        /// <param name="symbol">族类型</param>
        /// <returns>结构化的参数需求</returns>
        public static FamilyCreationRequirements AnalyzeRequirements(FamilySymbol symbol)
        {
            var requirements = new FamilyCreationRequirements
            {
                TypeId = symbol.Id.IntegerValue,
                FamilyName = symbol.FamilyName,
                TypeName = symbol.Name,
                PlacementType = symbol.Family.FamilyPlacementType.ToString(),
                IsSupported = true,
                RequiredParameters = new Dictionary<string, ParameterInfo>(),
                OptionalParameters = new Dictionary<string, ParameterInfo>(),
                Examples = new Dictionary<string, object>()
            };

            switch (symbol.Family.FamilyPlacementType)
            {
                case FamilyPlacementType.OneLevelBased:
                    AnalyzeOneLevelBased(requirements);
                    break;

                case FamilyPlacementType.TwoLevelsBased:
                    AnalyzeTwoLevelsBased(requirements);
                    break;

                case FamilyPlacementType.OneLevelBasedHosted:
                    AnalyzeOneLevelBasedHosted(requirements);
                    break;

                case FamilyPlacementType.WorkPlaneBased:
                    AnalyzeWorkPlaneBased(requirements);
                    break;

                case FamilyPlacementType.CurveBased:
                    AnalyzeCurveBased(requirements);
                    break;

                case FamilyPlacementType.ViewBased:
                    AnalyzeViewBased(requirements);
                    break;

                case FamilyPlacementType.CurveBasedDetail:
                    AnalyzeCurveBasedDetail(requirements);
                    break;

                case FamilyPlacementType.CurveDrivenStructural:
                    AnalyzeCurveDrivenStructural(requirements);
                    break;

                case FamilyPlacementType.Adaptive:
                    requirements.IsSupported = false;
                    requirements.UnsupportedReason = "自适应族需要多个自适应点作为输入，当前版本暂不支持。需要专门的点集输入接口。";
                    break;

                default:
                    requirements.IsSupported = false;
                    requirements.UnsupportedReason = $"未知的族放置类型: {symbol.Family.FamilyPlacementType}";
                    break;
            }

            return requirements;
        }

        /// <summary>
        /// 分析基于单个标高的族（OneLevelBased）
        /// </summary>
        private static void AnalyzeOneLevelBased(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationPoint"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "mm",
                Description = "放置点坐标",
                Example = new { x = 5000.0, y = 3000.0, z = 0.0 }
            };

            // 可选参数
            requirements.OptionalParameters["baseLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "关联标高的ElementId，不指定时自动查找最近标高",
                Example = 12345
            };

            requirements.OptionalParameters["baseOffset"] = new ParameterInfo
            {
                Type = "double",
                Unit = "mm",
                Description = "相对标高的偏移距离",
                Example = 1000.0
            };

            requirements.OptionalParameters["autoFindLevel"] = new ParameterInfo
            {
                Type = "bool",
                Unit = "",
                Description = "是否自动查找最近标高",
                Example = true
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationPoint = new { x = 5000.0, y = 3000.0, z = 0.0 },
                autoFindLevel = true,
                baseOffset = 500.0
            };
        }

        /// <summary>
        /// 分析基于两个标高的族（TwoLevelsBased）
        /// </summary>
        private static void AnalyzeTwoLevelsBased(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationPoint"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "mm",
                Description = "柱子底部定位点",
                Example = new { x = 2000.0, y = 1000.0, z = 0.0 }
            };

            requirements.RequiredParameters["baseLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "底部标高ElementId",
                Example = 12345
            };

            // 可选参数
            requirements.OptionalParameters["topLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "顶部标高ElementId，不指定时使用上一层标高",
                Example = 12346
            };

            requirements.OptionalParameters["baseOffset"] = new ParameterInfo
            {
                Type = "double",
                Unit = "mm",
                Description = "底部偏移",
                Example = 0.0
            };

            requirements.OptionalParameters["topOffset"] = new ParameterInfo
            {
                Type = "double",
                Unit = "mm",
                Description = "顶部偏移",
                Example = 0.0
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationPoint = new { x = 2000.0, y = 1000.0, z = 0.0 },
                baseLevelId = 12345,
                topLevelId = 12346,
                baseOffset = 0.0,
                topOffset = 0.0
            };
        }

        /// <summary>
        /// 分析基于单个标高和宿主的族（OneLevelBasedHosted）
        /// </summary>
        private static void AnalyzeOneLevelBasedHosted(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationPoint"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "mm",
                Description = "门窗放置点（将投影到宿主上）",
                Example = new { x = 5000.0, y = 2000.0, z = 1000.0 }
            };

            // 可选参数
            requirements.OptionalParameters["hostElementId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "宿主元素ID（墙、楼板等），不指定时自动查找",
                Example = 54321
            };

            requirements.OptionalParameters["baseLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "关联标高",
                Example = 12345
            };

            requirements.OptionalParameters["autoFindHost"] = new ParameterInfo
            {
                Type = "bool",
                Unit = "",
                Description = "是否自动查找宿主",
                Example = true
            };

            requirements.OptionalParameters["searchRadius"] = new ParameterInfo
            {
                Type = "double",
                Unit = "mm",
                Description = "自动查找宿主的搜索半径",
                Example = 5000.0
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationPoint = new { x = 5000.0, y = 2000.0, z = 1000.0 },
                autoFindHost = true,
                searchRadius = 5000.0
            };
        }

        /// <summary>
        /// 分析基于工作平面的族（WorkPlaneBased）
        /// </summary>
        private static void AnalyzeWorkPlaneBased(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationPoint"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "mm",
                Description = "在工作平面上的放置点",
                Example = new { x = 3000.0, y = 4000.0, z = 1200.0 }
            };

            // 可选参数
            requirements.OptionalParameters["faceDirection"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "normalized vector",
                Description = "面方向向量（归一化），通常指向法向方向",
                Example = new { x = 0.0, y = 0.0, z = 1.0 }
            };

            requirements.OptionalParameters["handDirection"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "normalized vector",
                Description = "手方向向量（归一化），定义族的X轴方向",
                Example = new { x = 1.0, y = 0.0, z = 0.0 }
            };

            requirements.OptionalParameters["hostCategories"] = new ParameterInfo
            {
                Type = "string[]",
                Unit = "BuiltInCategory names",
                Description = "允许的宿主类别，如[\"OST_Walls\", \"OST_Floors\", \"OST_Ceilings\"]",
                Example = new[] { "OST_Floors", "OST_Walls" }
            };

            requirements.OptionalParameters["autoFindHost"] = new ParameterInfo
            {
                Type = "bool",
                Unit = "",
                Description = "是否自动查找宿主面",
                Example = true
            };

            requirements.OptionalParameters["searchRadius"] = new ParameterInfo
            {
                Type = "double",
                Unit = "mm",
                Description = "查找宿主面的搜索半径",
                Example = 1000.0
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationPoint = new { x = 3000.0, y = 4000.0, z = 1200.0 },
                autoFindHost = true,
                hostCategories = new[] { "OST_Floors", "OST_Walls" }
            };
        }

        /// <summary>
        /// 分析基于线且在工作平面上的族（CurveBased）
        /// </summary>
        private static void AnalyzeCurveBased(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationLine"] = new ParameterInfo
            {
                Type = "JZLine",
                Unit = "mm",
                Description = "基准线段",
                Example = new
                {
                    p0 = new { x = 0.0, y = 0.0, z = 0.0 },
                    p1 = new { x = 5000.0, y = 0.0, z = 0.0 }
                }
            };

            // 可选参数
            requirements.OptionalParameters["baseLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "关联标高，不指定时需要宿主面",
                Example = 12345
            };

            requirements.OptionalParameters["hostCategories"] = new ParameterInfo
            {
                Type = "string[]",
                Unit = "BuiltInCategory names",
                Description = "当不指定标高时，用于查找宿主面的类别",
                Example = new[] { "OST_Floors", "OST_Walls", "OST_Ceilings" }
            };

            requirements.OptionalParameters["autoFindLevel"] = new ParameterInfo
            {
                Type = "bool",
                Unit = "",
                Description = "是否自动查找最近标高",
                Example = true
            };

            // 示例
            requirements.Examples["with_level"] = new
            {
                typeId = requirements.TypeId,
                locationLine = new
                {
                    p0 = new { x = 0.0, y = 0.0, z = 0.0 },
                    p1 = new { x = 5000.0, y = 0.0, z = 0.0 }
                },
                autoFindLevel = true
            };

            requirements.Examples["with_host"] = new
            {
                typeId = requirements.TypeId,
                locationLine = new
                {
                    p0 = new { x = 0.0, y = 0.0, z = 1200.0 },
                    p1 = new { x = 5000.0, y = 0.0, z = 1200.0 }
                },
                hostCategories = new[] { "OST_Floors" }
            };
        }

        /// <summary>
        /// 分析基于视图的族（ViewBased）
        /// </summary>
        private static void AnalyzeViewBased(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationPoint"] = new ParameterInfo
            {
                Type = "JZPoint",
                Unit = "mm",
                Description = "视图中的放置点（2D坐标，Z值忽略）",
                Example = new { x = 2000.0, y = 1500.0, z = 0.0 }
            };

            requirements.RequiredParameters["viewId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "目标2D视图的ElementId",
                Example = 67890
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationPoint = new { x = 2000.0, y = 1500.0, z = 0.0 },
                viewId = 67890
            };
        }

        /// <summary>
        /// 分析基于线且在特定视图中的族（CurveBasedDetail）
        /// </summary>
        private static void AnalyzeCurveBasedDetail(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationLine"] = new ParameterInfo
            {
                Type = "JZLine",
                Unit = "mm",
                Description = "详图线段（在视图平面内）",
                Example = new
                {
                    p0 = new { x = 1000.0, y = 500.0, z = 0.0 },
                    p1 = new { x = 3000.0, y = 500.0, z = 0.0 }
                }
            };

            requirements.RequiredParameters["viewId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "目标2D视图的ElementId",
                Example = 67890
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationLine = new
                {
                    p0 = new { x = 1000.0, y = 500.0, z = 0.0 },
                    p1 = new { x = 3000.0, y = 500.0, z = 0.0 }
                },
                viewId = 67890
            };
        }

        /// <summary>
        /// 分析结构曲线驱动的族（CurveDrivenStructural）
        /// </summary>
        private static void AnalyzeCurveDrivenStructural(FamilyCreationRequirements requirements)
        {
            // 必需参数
            requirements.RequiredParameters["locationLine"] = new ParameterInfo
            {
                Type = "JZLine",
                Unit = "mm",
                Description = "结构梁/支撑的轴线",
                Example = new
                {
                    p0 = new { x = 0.0, y = 0.0, z = 3000.0 },
                    p1 = new { x = 8000.0, y = 0.0, z = 3000.0 }
                }
            };

            requirements.RequiredParameters["baseLevelId"] = new ParameterInfo
            {
                Type = "int",
                Unit = "ElementId",
                Description = "参照标高",
                Example = 12345
            };

            // 示例
            requirements.Examples["typical"] = new
            {
                typeId = requirements.TypeId,
                locationLine = new
                {
                    p0 = new { x = 0.0, y = 0.0, z = 3000.0 },
                    p1 = new { x = 8000.0, y = 0.0, z = 3000.0 }
                },
                baseLevelId = 12345
            };
        }
    }
}