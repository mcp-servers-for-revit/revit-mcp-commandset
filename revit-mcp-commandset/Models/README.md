# Models 数据模型层

## 目录结构

```
Models/
├── Common/                 # 通用模型
│   ├── AIResult.cs        # 统一 API 返回格式
│   ├── ElementOperationResponse.cs  # 操作响应数据格式
│   └── CreationRequirements.cs     # 创建要求参数
└── Geometry/              # 几何模型
    ├── JZPoint.cs         # 点模型（单位：mm）
    ├── JZLine.cs          # 线模型
    ├── JZPlane.cs         # 平面模型
    ├── JZFace.cs          # 面模型
    ├── BoundingBoxInfo.cs # 包围盒信息
    └── LocationInfo.cs    # 位置信息（点/线统一）
```

## Common 通用模型

### AIResult

**用途**：所有 MCP 命令的统一返回格式

```csharp
public class AIResult<T>
{
    public bool Success { get; set; }      // 操作是否成功
    public string Message { get; set; }    // 操作消息
    public T Response { get; set; }        // 响应数据
}
```

**使用场景**：
- 成功时：`Success=true`，`Response` 包含实际数据
- 失败时：`Success=false`，`Message` 包含错误信息

### ElementOperationResponse

**用途**：v2.0 操作类命令的统一响应格式

```csharp
public class ElementOperationResponse
{
    public int ProcessedCount { get; set; }    // 处理元素数量
    public int SuccessCount { get; set; }      // 成功数量
    public int FailedCount { get; set; }       // 失败数量
    public List<ElementResult> Results { get; set; }  // 详细结果
}
```

**适用命令**：
- operate_element_visual
- operate_element_visibility
- operate_element_transform
- operate_element_modify

### CreationRequirements

**用途**：元素创建要求参数模型

**核心字段**：
- 族类型要求
- 宿主要求
- 参数约束
- 几何限制

## Geometry 几何模型

### 坐标系统约定

- **单位**：毫米 (mm)
- **坐标系**：Revit 项目坐标系
- **转换公式**：Revit 内部单位 × 304.8 = 毫米

### JZPoint

**用途**：三维点坐标

```csharp
public class JZPoint
{
    public double X { get; set; }  // X坐标 (mm)
    public double Y { get; set; }  // Y坐标 (mm)
    public double Z { get; set; }  // Z坐标 (mm)
}
```

### JZLine

**用途**：线段定义

```csharp
public class JZLine
{
    public JZPoint P0 { get; set; }  // 起点
    public JZPoint P1 { get; set; }  // 终点
}
```

### JZPlane

**用途**：平面定义（用于 Mirror 等操作）

```csharp
public class JZPlane
{
    public JZPoint Origin { get; set; }   // 原点
    public JZVector Normal { get; set; }  // 法向量（归一化）
}
```

### BoundingBoxInfo

**用途**：元素包围盒信息

```csharp
public class BoundingBoxInfo
{
    public JZPoint Min { get; set; }  // 最小点
    public JZPoint Max { get; set; }  // 最大点
}
```

### LocationInfo

**用途**：统一的位置信息（自动识别点/线）

```csharp
public class LocationInfo
{
    public JZPoint Point { get; set; }  // 点位置（如门、窗）
    public JZLine Line { get; set; }    // 线位置（如墙体）
}
```

**智能识别**：
- 门窗等点定位元素：`Point` 有值，`Line` 为 null
- 墙体等线定位元素：`Line` 有值，`Point` 为 null

## 使用规范

### 单位转换

所有几何模型使用毫米作为单位，与 Revit 内部单位转换时：

- **长度**：`Revit单位 × 304.8 = 毫米`
- **角度**：度数制，转换时使用 `Math.PI / 180`

### 命名规范

- 模型类名：使用 PascalCase
- 属性名：使用 PascalCase
- JsonProperty：使用 camelCase（与服务端 Zod schema 一致）

### 数据验证

创建模型实例时应验证：
- 几何数据的有效性（非 null、非 NaN）
- 单位的正确性（已转换为毫米）
- 坐标系的一致性（项目坐标系）

## 相关文档

- **参数转换**：参考 Utils/ParameterHelper.cs
- **节点化架构**：参考 Features/ElementFilter/README.md
- **操作响应**：参考 Features/Element*/README.md