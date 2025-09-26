using Newtonsoft.Json;

namespace RevitMCPCommandSet.Models.Geometry
{
    /// <summary>
    /// 平面定义（用于镜像操作）
    /// </summary>
    public class JZPlane
    {
        /// <summary>
        /// 平面原点（毫米）
        /// </summary>
        [JsonProperty("origin")]
        public JZPoint Origin { get; set; }

        /// <summary>
        /// 平面法向量（归一化向量）
        /// </summary>
        [JsonProperty("normal")]
        public JZVector Normal { get; set; }

        /// <summary>
        /// 转换为Revit Plane对象
        /// </summary>
        public Autodesk.Revit.DB.Plane ToRevitPlane()
        {
            var origin = new Autodesk.Revit.DB.XYZ(
                Origin.X / 304.8,
                Origin.Y / 304.8,
                Origin.Z / 304.8
            );

            var normal = new Autodesk.Revit.DB.XYZ(
                Normal.X,
                Normal.Y,
                Normal.Z
            ).Normalize();

            return Autodesk.Revit.DB.Plane.CreateByNormalAndOrigin(normal, origin);
        }
    }

    /// <summary>
    /// 向量定义
    /// </summary>
    public class JZVector
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        [JsonProperty("z")]
        public double Z { get; set; }
    }
}