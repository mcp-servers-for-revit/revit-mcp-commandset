# Revit 族创建工具模块

本模块提供了完整的 Revit 族实例创建功能，包括参数验证、智能提示和多种族类型的统一创建接口。

## 📁 模块结构

```
Utils/FamilyCreation/
├── FamilyInstanceCreator.cs     # 底层族创建器（核心 Revit API 操作）
├── FamilyInstanceService.cs     # 高层服务类（智能创建和验证）
└── README.md                    # 本说明文档
```

## 🏗️ 架构设计

### 双层设计模式

本模块采用**服务层 + 核心层**的双层架构：

```
┌─────────────────────────────┐
│   FamilyInstanceService     │  ← 服务层（智能功能）
│   ├─ 参数验证               │
│   ├─ 失败分析               │
│   └─ 自动建议               │
├─────────────────────────────┤
│   FamilyInstanceCreator     │  ← 核心层（Revit API）
│   ├─ Setup 方法链           │
│   ├─ Create 创建            │
│   └─ 各种族类型支持         │
└─────────────────────────────┘
```

### 职责分工

| 类名 | 职责 | 特点 |
|------|------|------|
| **FamilyInstanceCreator** | Revit API 直接操作 | • 纯技术实现<br>• 链式调用<br>• 专注创建逻辑<br>• 完整的族类型支持 |
| **FamilyInstanceService** | 业务逻辑和智能功能 | • 参数验证<br>• 参数分析<br>• 失败即建议<br>• 用户友好的API |

---

## 🔧 FamilyInstanceCreator（核心创建器）

### 概述
底层族创建器，直接封装 Revit API，提供链式调用接口。专注于族实例的创建逻辑，不包含参数分析和建议功能。

### 支持的族类型
支持 8 种主要的族放置类型：

1. **OneLevelBased** - 基于单个标高的族（如家具、设备）
2. **OneLevelBasedHosted** - 需要宿主的族（如门、窗）
3. **TwoLevelsBased** - 基于两个标高的族（如柱子）
4. **WorkPlaneBased** - 基于工作面的族（如墙贴设备）
5. **CurveBased** - 基于曲线的族（如栏杆）
6. **CurveBasedDetail** - 基于曲线的详图族（如标注线）
7. **CurveDrivenStructural** - 基于曲线的结构族（如梁）
8. **ViewBased** - 基于视图的族（如图例、标注）

### 核心 API

#### 构造函数
```csharp
public FamilyInstanceCreator(Document document)
```

#### Setup 方法（链式调用）
```csharp
// 1. 基于单个标高的族
creator.SetupOneLevelBased(FamilySymbol symbol, XYZ locationPoint, Level baseLevel = null)

// 2. 需要宿主的族（门窗等）
creator.SetupOneLevelBasedHosted(FamilySymbol symbol, XYZ locationPoint, Level baseLevel = null)

// 3. 基于两个标高的族（柱子等）
creator.SetupTwoLevelsBased(FamilySymbol symbol, XYZ locationPoint, Level baseLevel,
                           Level topLevel = null, double baseOffset = -1, double topOffset = -1)

// 4. 基于工作面的族
creator.SetupWorkPlaneBased(FamilySymbol symbol, XYZ locationPoint)

// 5. 基于曲线的族
creator.SetupCurveBased(FamilySymbol symbol, Line locationLine)
creator.SetupCurveDrivenStructural(FamilySymbol symbol, Line locationLine, Level baseLevel)
creator.SetupCurveBasedDetail(FamilySymbol symbol, Line locationLine, View view)

// 6. 基于视图的族
creator.SetupViewBased(FamilySymbol symbol, XYZ locationPoint, View view)
```

#### 创建方法
```csharp
public FamilyInstance Create()  // 执行创建并返回族实例
public void Reset()             // 重置创建器状态
```

### 使用示例
```csharp
// 基本用法
var creator = new FamilyInstanceCreator(doc);
var instance = creator
    .SetupOneLevelBased(familySymbol, locationPoint, level)
    .Create();

// 复杂族创建（门窗）
var door = creator
    .SetupOneLevelBasedHosted(doorSymbol, doorLocation, level)
    .Create();
```

### 注意事项
- 所有坐标使用 Revit 内部单位（英尺）
- 需要在有效的事务（Transaction）中调用 `Create()` 方法
- 族类型必须已激活（`symbol.Activate()`）
- 如需参数建议，请使用 FamilyInstanceService.AnalyzeRequirements()

---

## 🧠 FamilyInstanceService（智能服务）

### 概述
高层服务类，在 FamilyInstanceCreator 基础上提供智能功能，包括参数验证、自动修复、失败分析等。

### 核心特性

#### 🎯 智能创建
- **失败即建议**：创建失败时自动提供正确的参数要求
- **参数验证**：创建前验证参数完整性
- **自动修复**：自动查找最近标高等智能修复

#### 🔍 参数分析
- **结构化建议**：提供详细的参数要求说明
- **示例参数**：每种族类型都有完整的参数示例
- **单位说明**：明确标注参数单位和转换规则

### 核心 API

#### 构造函数
```csharp
public FamilyInstanceService(Document document)
```

#### 主要方法
```csharp
// 智能创建族实例
public CreateResult CreateInstance(FamilyCreationParameters parameters)

// 分析族类型参数要求
public FamilyCreationRequirements AnalyzeRequirements(int typeId)
```

### 使用示例

#### 智能创建
```csharp
var service = new FamilyInstanceService(doc);

var parameters = new FamilyCreationParameters
{
    TypeId = 12345,
    LocationPoint = new JZPoint(2000, 1500, 0),  // 毫米单位
    AutoFindLevel = true
};

var result = service.CreateInstance(parameters);

if (result.Success)
{
    Console.WriteLine($"创建成功，元素ID：{result.ElementId}");
}
else
{
    Console.WriteLine($"创建失败：{result.Message}");

    // 自动获取参数建议
    if (result.AdditionalInfo.ContainsKey("suggestion"))
    {
        var suggestion = result.AdditionalInfo["suggestion"] as FamilyCreationRequirements;
        Console.WriteLine("参数建议：");
        foreach (var param in suggestion.RequiredParameters)
        {
            Console.WriteLine($"- {param.Key}: {param.Value.Description}");
        }
    }
}
```

#### 获取参数建议
```csharp
var service = new FamilyInstanceService(doc);
var requirements = service.AnalyzeRequirements(typeId);

Console.WriteLine($"族名称：{requirements.FamilyName}");
Console.WriteLine("必需参数：");
foreach (var param in requirements.RequiredParameters)
{
    Console.WriteLine($"- {param.Key}: {param.Value.Description}");
    Console.WriteLine($"  示例：{param.Value.Example}");
}
```

### 参数验证功能

#### 自动修复
- **标高查找**：`autoFindLevel=true` 时自动查找最近标高
- **参数补全**：尽可能补全缺失的可选参数

#### 验证规则
不同族类型有不同的验证规则：

| 族类型 | 必需参数 | 验证规则 |
|--------|----------|----------|
| OneLevelBased | locationPoint | 基本位置验证 |
| OneLevelBasedHosted | locationPoint | + 宿主元素检查 |
| TwoLevelsBased | locationPoint, topLevelId | + 双标高验证 |
| WorkPlaneBased | locationPoint | + 工作面检查 |
| CurveBased | locationLine | + 线段有效性 |
| ViewBased | locationPoint, viewId | + 视图存在性 |

---

## 📊 数据结构

### FamilyCreationParameters（创建参数）
```csharp
public class FamilyCreationParameters
{
    public int TypeId { get; set; }                     // 族类型ID（必需）
    public JZPoint LocationPoint { get; set; }          // 位置点（毫米）
    public JZLine LocationLine { get; set; }            // 位置线（毫米）

    // 标高相关
    public int BaseLevelId { get; set; }                // 基准标高ID
    public int TopLevelId { get; set; }                 // 顶部标高ID
    public double BaseOffset { get; set; }              // 基准偏移（毫米）
    public double TopOffset { get; set; }               // 顶部偏移（毫米）
    public bool AutoFindLevel { get; set; }             // 自动查找标高

    // 宿主相关
    public int HostElementId { get; set; }              // 宿主元素ID
    public bool AutoFindHost { get; set; }              // 自动查找宿主
    public double SearchRadius { get; set; }            // 搜索半径（毫米）
    public string[] HostCategories { get; set; }        // 宿主类别过滤

    // 方向相关
    public JZPoint FaceDirection { get; set; }          // 面法向量
    public JZPoint HandDirection { get; set; }          // 手向量

    // 视图相关
    public int ViewId { get; set; }                     // 视图ID
}
```

### FamilyCreationRequirements（参数要求）
```csharp
public class FamilyCreationRequirements
{
    public int TypeId { get; set; }                                         // 族类型ID
    public string FamilyName { get; set; }                                  // 族名称
    public Dictionary<string, ParameterInfo> RequiredParameters { get; set; } // 必需参数
    public Dictionary<string, ParameterInfo> OptionalParameters { get; set; } // 可选参数
}

public class ParameterInfo
{
    public string Type { get; set; }                    // 参数类型
    public string Description { get; set; }             // 参数描述
    public object Example { get; set; }                 // 示例值
}
```

### CreateResult（创建结果）
```csharp
public class CreateResult
{
    public bool Success { get; set; }                   // 是否成功
    public string Message { get; set; }                 // 结果消息
    public int ElementId { get; set; }                  // 创建的元素ID
    public string ElementType { get; set; }             // 元素类型
    public Dictionary<string, object> AdditionalInfo { get; set; } // 附加信息（包含建议）
}
```

---

## 🔄 单位转换

### 坐标单位
- **输入**：毫米（mm）- JZPoint、JZLine 使用毫米
- **内部**：英尺（ft）- Revit API 内部单位
- **转换**：`毫米 ÷ 304.8 = 英尺`

### 自动转换
```csharp
// JZPoint 自动转换为 XYZ
JZPoint jzPoint = new JZPoint(3048, 1524, 0);  // 毫米
XYZ revitPoint = JZPoint.ToXYZ(jzPoint);       // 转换为英尺 (10, 5, 0)
```

---

## 💡 最佳实践

### 1. 事务管理
```csharp
using (Transaction trans = new Transaction(doc, "Create Family Instance"))
{
    trans.Start();

    var result = service.CreateInstance(parameters);

    if (result.Success)
        trans.Commit();
    else
        trans.RollBack();
}
```

### 2. 错误处理
```csharp
var result = service.CreateInstance(parameters);
if (!result.Success)
{
    // 检查是否有参数建议
    if (result.AdditionalInfo?.ContainsKey("suggestion") == true)
    {
        var suggestion = result.AdditionalInfo["suggestion"] as FamilyCreationRequirements;
        // 根据建议调整参数后重试
    }
}
```

### 3. 性能优化
```csharp
// 批量创建时复用服务实例
var service = new FamilyInstanceService(doc);
foreach (var param in parametersList)
{
    var result = service.CreateInstance(param);
    // 处理结果...
}
```

### 4. 参数预检查
```csharp
// 创建前先获取参数要求
var requirements = service.AnalyzeRequirements(typeId);
// 根据要求构建完整的参数
var parameters = BuildParametersFromRequirements(requirements);
var result = service.CreateInstance(parameters);
```

---

## 🚨 注意事项

### 线程安全
- **仅限主线程**：所有方法必须在 Revit 主线程中调用
- **事务环境**：Create 操作必须在有效的事务中执行

### 族类型要求
- 族必须已加载到文档中
- FamilySymbol 必须已激活（`symbol.Activate()`）

### 参数有效性
- 所有 ElementId 必须存在于当前文档中
- 坐标值必须在合理范围内
- 线段不能为零长度

### 版本兼容性
- 支持 Revit 2020-2025
- 部分 API 可能在不同版本间有差异

---

## 🔗 相关文件

- **命令层**：`Features/FamilyInstanceCreation/`
  - `CreateFamilyInstanceCommand.cs` - MCP 创建命令
  - `GetFamilyCreationSuggestionCommand.cs` - MCP 建议命令
- **数据模型**：`Models/Common/`
  - `FamilyCreationParameters.cs` - 创建参数
  - `FamilyCreationRequirements.cs` - 参数要求
- **几何模型**：`Models/Geometry/`
  - `JZPoint.cs` - 三维点
  - `JZLine.cs` - 三维线

---

## 📈 版本历史

- **v1.0** - 初始版本，基础族创建功能
- **v1.1** - 添加智能验证和建议功能
- **v2.0** - 重构为双层架构，提升可维护性
- **v2.1** - 清理 FamilyInstanceCreator，移除冗余的建议功能

---

*本文档随代码更新，如有疑问请参考源码或联系开发团队。*