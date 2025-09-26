using RevitMCPCommandSet.Models.Geometry;

namespace RevitMCPCommandSet.Utils
{
    public static class ProjectUtils
    {
        /// <summary>
        /// 获取元素指定法向量的平面轮廓
        /// </summary>
        /// <param name="element"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static List<JZFace> GetElementProfile(this Element element, XYZ normal = null)
        {
            var profiles = new List<JZFace>();
            var solid = element.GetElementSolid();
            if (solid == null)
            {
                return null;
            }

            var planarFaces = solid.FindFace(normal ?? new XYZ(0, 0, -1));
            if (planarFaces == null)
            {
                return null;
            }

            // 提取底面的所有JZFace（外环+洞）
            foreach (var planarFace in planarFaces)
            {
                var faces = planarFace.ToJZFace();
                if (faces != null)
                {
                    profiles.AddRange(faces);
                }
            }

            return profiles;
        }

        /// <summary>
        /// 获取指定Revit元素的第一个主Solid几何体
        /// </summary>
        /// <param name="element">要提取Solid的Revit元素</param>
        /// <returns>第一个体积大于0的Solid实例，若无则返回null</returns>
        public static Solid GetElementSolid(this Element element)
        {
            #region Step1 获取元素的几何表示

            // 使用Options配置，ComputeReferences用于需要几何引用，IncludeNonVisibleObjects为否仅取可见体
            GeometryElement geomElem = element.get_Geometry(new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = false
            });
            if (geomElem == null) return null;

            #endregion

            #region Step2 遍历所有一级GeometryObject

            foreach (GeometryObject geomObj in geomElem)
            {
                // 1. 如果对象本身是Solid且体积大于0，直接返回
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    return solid;
                }
                // 2. 如果遇到几何实例（GeometryInstance），进一步获取其实例内部的几何体并遍历
                else if (geomObj is GeometryInstance instance)
                {
                    GeometryElement instanceGeom = instance.GetInstanceGeometry();
                    foreach (GeometryObject instanceObj in instanceGeom)
                    {
                        if (instanceObj is Solid instanceSolid && instanceSolid.Volume > 0)
                        {
                            return instanceSolid;
                        }
                    }
                }
            }

            #endregion

            // 如果遍历后均未找到符合要求的Solid，则返回null
            return null;
        }

        /// <summary>
        /// 获得指定表面
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<PlanarFace> FindFace(this Solid solid, XYZ normal)
        {
            var planarFaces = new List<PlanarFace>();
            Solid pSolid = solid;
            FaceArray faces = pSolid.Faces;
            foreach (Face pFace in faces)
            {
                if (pFace is PlanarFace)
                {
                    //Face 强制转换为 PlanarFace
                    PlanarFace pPlanarFace = pFace as PlanarFace;

                    //ComputeNormal是Face的方法，返回的是在某个指定点的法向量
                    //判断pPlanarFace的外法向量和所选面的法向量normal是否总是相同
                    if (pPlanarFace.ComputeNormal(new UV(0.5, 0.5)).IsAlmostEqualTo(normal))
                    {
                        planarFaces.Add(pPlanarFace);
                    }
                }
            }

            return planarFaces;
        }

        /// <summary>
        /// 将PlanarFace的所有边界CurveLoop转换为JZFace列表。
        /// 每个CurveLoop生成一个独立的JZFace。<br/>
        /// 【假设CurveLoop全部由Line组成，否则返回null】<br/>
        /// 单位转换：ft -> mm (×304.8)
        /// </summary>
        /// <param name="planarFace">Revit的PlanarFace</param>
        /// <returns>JZFace对象列表</returns>
        public static List<JZFace> ToJZFace(this PlanarFace planarFace)
        {
            if (planarFace == null)
                return null;

            var faces = new List<JZFace>();

            // 获取所有CurveLoop
            IList<CurveLoop> loops = planarFace.GetEdgesAsCurveLoops();
            if (loops == null || loops.Count == 0)
                return null;

            // 每个CurveLoop生成一个独立的JZFace
            foreach (var curveLoop in loops)
            {
                var face = new JZFace();
                var loop = new List<JZLine>();

                foreach (Curve curve in curveLoop)
                {
                    if (curve is Line line)
                    {
                        // 单位转换：ft -> mm (×304.8)
                        loop.Add(new JZLine(
                            new JZPoint(line.GetEndPoint(0).X * 304.8, line.GetEndPoint(0).Y * 304.8, line.GetEndPoint(0).Z * 304.8),
                            new JZPoint(line.GetEndPoint(1).X * 304.8, line.GetEndPoint(1).Y * 304.8, line.GetEndPoint(1).Z * 304.8)
                        ));
                    }
                    else
                        return null;  // 如果不是Line，返回null
                }

                face.OuterLoop = loop;
                faces.Add(face);
            }

            return faces;
        }
        
        /// <summary>
        /// 参数化驱动族实例
        /// </summary>
        /// <param name="element">要修改的族实例</param>
        /// <param name="paramInfo">参数名和值字典</param>
        public static void EditParameter(this Document doc, Element element, Dictionary<string, string> paramInfo)
        {
            foreach (var pair in paramInfo)
            {
                Parameter parameter = doc.GetParameter(element, pair.Key);

                if (parameter != null)
                {
                    //如果不是只读
                    if (!parameter.IsReadOnly)
                    {
                        StorageType storageType = parameter.StorageType;
                        //如果是string类型存储的
                        if (storageType == StorageType.String)
                        {
                            parameter.Set(pair.Value);
                        }
                        else if (storageType == StorageType.Integer)
                        {
                            if (pair.Value == "是")
                            {
                                parameter.Set(1);

                            }
                            else if (pair.Value == "否")
                            {
                                parameter.Set(0);
                            }
                            else
                            {
                                if (int.TryParse(pair.Value, out int value))
                                {
                                    parameter.Set(value);
                                }
                            }
                        }
                        else
                        {
                            parameter.SetValueString(pair.Value);
                        }
                    }
                }
                //如果没有当前参数
                else
                {
                    continue;
                }
            }
        }
        
        /// <summary>
        /// 获取对应参数
        /// </summary>
        /// <param name="doc">项目文档</param>
        /// <param name="ele">元素</param>
        /// <param name="pName">参数名称</param>
        /// <returns>参数名称对应的参数</returns>
        public static Parameter GetParameter(this Document doc, Element ele, string pName)
        {
            Element typeEle = doc.GetElement(ele.GetTypeId());
            //判断是实例参数还是类型参数
            //如果是实例参数
            if (ele.LookupParameter(pName) != null)
            {
                return ele.LookupParameter(pName);
            }
            //如果是类型参数
            else if (typeEle != null)
            {
                if (typeEle.LookupParameter(pName) != null)
                {
                    return typeEle.LookupParameter(pName);
                }
                //如果没有当前参数
                else
                {
                    return null;
                }
            }
            //如果没有当前参数
            else
            {
                return null;
            }
        }
        
        /// <summary>
        /// 判断参数是否为可写入的尺寸参数
        /// </summary>
        private static bool IsDimensionParameter(Parameter param)
        {
            // 判断参数是否为尺寸相关的类型
            bool isDimensionType = param.Definition.ParameterType == ParameterType.Length ||
                                   param.Definition.ParameterType == ParameterType.Angle ||
                                   param.Definition.ParameterType == ParameterType.Area ||
                                   param.Definition.ParameterType == ParameterType.Volume;

            // 只存储尺寸类型参数
            return isDimensionType;
        }

        /// <summary>
        /// 获取参数的值并将其转换为字符串
        /// </summary>
        private static string GetParameterValue(Parameter param)
        {
            // 根据参数的存储类型返回对应的值
            if (param.StorageType == StorageType.String)
            {
                return param.AsString();  // 如果是字符串类型参数，直接返回字符串值
            }
            else if (param.StorageType == StorageType.Integer)
            {
                return param.AsInteger().ToString();  // 如果是整数类型，返回整数值
            }
            else if (param.StorageType == StorageType.Double)
            {
                // 如果是双精度类型，返回以毫米为单位的值
                double valueInMm = UnitUtils.ConvertFromInternalUnits(param.AsDouble(), DisplayUnitType.DUT_MILLIMETERS);
                return valueInMm.ToString("0.###");  // 保留三位小数
            }
            else if (param.StorageType == StorageType.ElementId)
            {
                return param.AsElementId().IntegerValue.ToString();  // 返回 ElementId 的整数值
            }
            return string.Empty;  // 其他类型返回空字符串
        }

        
        
    }
}