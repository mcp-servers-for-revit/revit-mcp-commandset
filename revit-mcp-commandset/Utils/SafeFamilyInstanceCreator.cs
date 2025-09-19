using Autodesk.Revit.DB;
using RevitMCPCommandSet.Models.Common;
using RevitMCPCommandSet.Models.Geometry;

namespace RevitMCPCommandSet.Utils
{
    /// <summary>
    /// 安全的族实例创建器（封装FamilyInstanceCreator，避免UI阻塞）
    /// </summary>
    public class SafeFamilyInstanceCreator
    {
        private readonly Document doc;
        private readonly FamilyInstanceCreator creator;
        private CreateResult result;

        public SafeFamilyInstanceCreator(Document document)
        {
            doc = document ?? throw new ArgumentNullException(nameof(document));
            creator = new FamilyInstanceCreator(document);
            result = new CreateResult();
        }

        /// <summary>
        /// 创建族实例
        /// </summary>
        /// <param name="parameters">创建参数</param>
        /// <returns>创建结果</returns>
        public CreateResult CreateInstance(FamilyCreationParameters parameters)
        {
            result = new CreateResult();

            try
            {
                // 1. 获取并验证族类型
                var symbol = doc.GetElement(new ElementId(parameters.TypeId)) as FamilySymbol;
                if (symbol == null)
                {
                    result.Success = false;
                    result.Message = $"找不到族类型 ID: {parameters.TypeId}";
                    return result;
                }

                // 2. 检查自适应族支持
                if (symbol.Family.FamilyPlacementType == FamilyPlacementType.Adaptive)
                {
                    result.Success = false;
                    result.Message = "暂不支持自适应族类型，需要专门的点集输入接口";
                    return result;
                }

                // 3. 解析Revit对象和单位转换
                Level baseLevel = ResolveLevel(parameters.BaseLevelId, parameters.AutoFindLevel, parameters.LocationPoint);
                Level topLevel = parameters.TopLevelId > 0 ? doc.GetElement(new ElementId(parameters.TopLevelId)) as Level : null;
                View view = parameters.ViewId > 0 ? doc.GetElement(new ElementId(parameters.ViewId)) as View : null;
                Element hostElement = parameters.HostElementId > 0 ? doc.GetElement(new ElementId(parameters.HostElementId)) : null;

                // 单位转换：毫米转英尺（JZPoint.ToXYZ/JZLine.ToLine已包含/304.8转换）
                XYZ locationPoint = parameters.LocationPoint != null ? JZPoint.ToXYZ(parameters.LocationPoint) : null;
                Line locationLine = parameters.LocationLine != null ? JZLine.ToLine(parameters.LocationLine) : null;
                double baseOffsetFt = parameters.BaseOffset / 304.8;
                double topOffsetFt = parameters.TopOffset / 304.8;

                // 转换方向向量
                XYZ faceDirection = parameters.FaceDirection != null ?
                    new XYZ(parameters.FaceDirection.X, parameters.FaceDirection.Y, parameters.FaceDirection.Z).Normalize() : null;
                XYZ handDirection = parameters.HandDirection != null ?
                    new XYZ(parameters.HandDirection.X, parameters.HandDirection.Y, parameters.HandDirection.Z).Normalize() : null;

                // 4. 转换宿主类别
                BuiltInCategory[] hostCategories = ConvertHostCategories(parameters.HostCategories);

                // 5. 根据放置类型调用对应Setup方法
                bool setupSuccess = SetupCreatorByPlacementType(
                    symbol, locationPoint, locationLine, baseLevel, topLevel,
                    baseOffsetFt, topOffsetFt, view, faceDirection, handDirection,
                    hostCategories, hostElement, parameters);

                if (!setupSuccess)
                {
                    return result; // result中已设置错误信息
                }

                // 6. 创建实例
                FamilyInstance instance = creator.Create();

                if (instance != null)
                {
                    result.Success = true;
                    result.ElementId = instance.Id.IntegerValue;
                    result.ElementType = instance.Category.Name;
                    result.Message = "族实例创建成功";
                    result.AdditionalInfo = new Dictionary<string, object>
                    {
                        ["FamilyName"] = symbol.FamilyName,
                        ["TypeName"] = symbol.Name,
                        ["PlacementType"] = symbol.Family.FamilyPlacementType.ToString()
                    };
                }
                else
                {
                    result.Success = false;
                    result.Message = "创建失败，可能是宿主条件不满足或参数配置有误";
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"创建过程异常: {ex.Message}";
                System.Diagnostics.Trace.WriteLine($"[SafeFamilyInstanceCreator] 异常详情: {ex}");
            }
            finally
            {
                creator.Reset(); // 确保清理状态
            }

            return result;
        }

        /// <summary>
        /// 解析标高
        /// </summary>
        private Level ResolveLevel(int levelId, bool autoFind, JZPoint location)
        {
            if (levelId > 0)
            {
                return doc.GetElement(new ElementId(levelId)) as Level;
            }

            if (autoFind && location != null)
            {
                double heightFt = location.Z / 304.8;
                return FamilyInstanceCreator.GetNearestLevel(doc, heightFt);
            }

            return null;
        }

        /// <summary>
        /// 转换宿主类别字符串数组为BuiltInCategory数组
        /// </summary>
        private BuiltInCategory[] ConvertHostCategories(string[] categoryNames)
        {
            if (categoryNames == null || categoryNames.Length == 0)
                return null;

            var categories = new List<BuiltInCategory>();
            foreach (var name in categoryNames)
            {
                try
                {
                    // 尝试解析BuiltInCategory枚举
                    if (Enum.TryParse<BuiltInCategory>(name, true, out var category))
                    {
                        categories.Add(category);
                    }
                    else
                    {
                        System.Diagnostics.Trace.WriteLine($"[SafeFamilyInstanceCreator] 无法识别的类别名称: {name}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine($"[SafeFamilyInstanceCreator] 转换类别异常: {name}, {ex.Message}");
                }
            }

            return categories.Count > 0 ? categories.ToArray() : null;
        }

        /// <summary>
        /// 根据放置类型设置创建器
        /// </summary>
        private bool SetupCreatorByPlacementType(
            FamilySymbol symbol, XYZ locationPoint, Line locationLine,
            Level baseLevel, Level topLevel, double baseOffsetFt, double topOffsetFt,
            View view, XYZ faceDirection, XYZ handDirection,
            BuiltInCategory[] hostCategories, Element hostElement, FamilyCreationParameters parameters)
        {
            try
            {
                switch (symbol.Family.FamilyPlacementType)
                {
                    case FamilyPlacementType.OneLevelBased:
                        if (locationPoint == null)
                        {
                            result.Success = false;
                            result.Message = "OneLevelBased族必须指定locationPoint";
                            return false;
                        }
                        creator.SetupOneLevelBased(symbol, locationPoint, baseLevel);
                        if (Math.Abs(baseOffsetFt) > 0.001) creator.BaseOffset = baseOffsetFt;
                        break;

                    case FamilyPlacementType.OneLevelBasedHosted:
                        if (locationPoint == null)
                        {
                            result.Success = false;
                            result.Message = "OneLevelBasedHosted族必须指定locationPoint";
                            return false;
                        }

                        // 如果指定了宿主元素，需要特殊处理（此处简化处理）
                        if (hostElement != null)
                        {
                            // 注意：FamilyInstanceCreator的SetupOneLevelBasedHosted方法会自动查找宿主
                            // 这里我们先按原有逻辑处理，后续可以扩展支持手动指定宿主
                            System.Diagnostics.Trace.WriteLine($"[SafeFamilyInstanceCreator] 忽略手动指定的宿主元素，使用自动查找");
                        }

                        if (!parameters.AutoFindHost && hostElement == null)
                        {
                            result.Success = false;
                            result.Message = "需要指定宿主元素或启用自动查找";
                            return false;
                        }

                        creator.SetupOneLevelBasedHosted(symbol, locationPoint, baseLevel);
                        break;

                    case FamilyPlacementType.TwoLevelsBased:
                        if (locationPoint == null)
                        {
                            result.Success = false;
                            result.Message = "TwoLevelsBased族必须指定locationPoint";
                            return false;
                        }
                        if (baseLevel == null)
                        {
                            result.Success = false;
                            result.Message = "TwoLevelsBased族必须指定baseLevelId";
                            return false;
                        }
                        creator.SetupTwoLevelsBased(symbol, locationPoint, baseLevel, topLevel, baseOffsetFt, topOffsetFt);
                        break;

                    case FamilyPlacementType.WorkPlaneBased:
                        if (locationPoint == null)
                        {
                            result.Success = false;
                            result.Message = "WorkPlaneBased族必须指定locationPoint";
                            return false;
                        }
                        creator.SetupWorkPlaneBased(symbol, locationPoint, faceDirection, hostCategories ?? new BuiltInCategory[0]);
                        if (handDirection != null) creator.HandDirection = handDirection;
                        break;

                    case FamilyPlacementType.CurveBased:
                        if (locationLine == null)
                        {
                            result.Success = false;
                            result.Message = "CurveBased族必须指定locationLine";
                            return false;
                        }
                        creator.SetupCurveBased(symbol, locationLine, baseLevel, hostCategories ?? new BuiltInCategory[0]);
                        break;

                    case FamilyPlacementType.ViewBased:
                        if (locationPoint == null)
                        {
                            result.Success = false;
                            result.Message = "ViewBased族必须指定locationPoint";
                            return false;
                        }
                        if (view == null)
                        {
                            result.Success = false;
                            result.Message = "ViewBased族必须指定有效的viewId";
                            return false;
                        }
                        creator.SetupViewBased(symbol, locationPoint, view);
                        break;

                    case FamilyPlacementType.CurveBasedDetail:
                        if (locationLine == null)
                        {
                            result.Success = false;
                            result.Message = "CurveBasedDetail族必须指定locationLine";
                            return false;
                        }
                        if (view == null)
                        {
                            result.Success = false;
                            result.Message = "CurveBasedDetail族必须指定有效的viewId";
                            return false;
                        }
                        creator.SetupCurveBasedDetail(symbol, locationLine, view);
                        break;

                    case FamilyPlacementType.CurveDrivenStructural:
                        if (locationLine == null)
                        {
                            result.Success = false;
                            result.Message = "CurveDrivenStructural族必须指定locationLine";
                            return false;
                        }
                        if (baseLevel == null)
                        {
                            result.Success = false;
                            result.Message = "CurveDrivenStructural族必须指定baseLevelId";
                            return false;
                        }
                        creator.SetupCurveDrivenStructural(symbol, locationLine, baseLevel);
                        break;

                    default:
                        result.Success = false;
                        result.Message = $"不支持的族放置类型: {symbol.Family.FamilyPlacementType}";
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Setup失败: {ex.Message}";
                System.Diagnostics.Trace.WriteLine($"[SetupCreatorByPlacementType] {ex}");
                return false;
            }
        }
    }
}