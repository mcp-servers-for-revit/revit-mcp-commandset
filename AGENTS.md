# Revit MCP CommandSet 项目架构文档

## 项目概述

本项目是 Revit MCP 生态系统的核心组件，负责在 Revit 端实现与 AI 助手的通信桥梁。通过统一的命令模式，为 LLM 提供访问和操作 Revit 模型的能力。

### 核心特性
- 🔗 **AI-BIM 连接**: 连接大语言模型与 Revit 软件的桥梁
- 🏗️ **统一架构**: 基于 RevitMCPSDK 的标准化开发模式
- 📊 **节点化架构**: 结构化数据节点，AI友好的信息组织方式
- ⚡ **异步处理**: 支持复杂操作的异步执行和超时控制
- 🔧 **CRUD 完整**: 提供元素创建、查询、更新、删除的完整功能

## 技术架构

### 核心依赖
- **RevitMCPSDK**: 版本 `$(RevitVersion).*` - 提供统一的开发规范
- **Revit API**: 支持 Revit 2020-2025 多版本
- **Newtonsoft.Json**: JSON 序列化和数据交换
- **.NET Framework 4.8** (R20-R24) / **.NET 8** (R25+)

### 双层架构设计

项目采用 **Command + EventHandler** 双层架构：

```
MCP Client (AI/LLM)
    ↓ JSON Parameters
[ExternalEventCommandBase] ← 命令入口层
    ↓ 参数解析 & 事件触发
[IExternalEventHandler] ← Revit功能实现层
    ↓ Revit API 调用
Revit Application
```

## 目录结构

```
revit-mcp-commandset/
├── Features/                  # 功能模块目录（按功能组织）
│   ├── ElementFilter/         # 元素过滤功能模块（节点化架构v2.0）
│   │   ├── AIElementFilterCommand.cs
│   │   ├── AIElementFilterEventHandler.cs
│   │   ├── FieldBuilders/    # 字段构建器（节点化核心，包含Core、Geometry等构建器）
│   │   └── Models/           # 元素过滤模型
│   │       ├── FilterSetting.cs
│   │       ├── GeometryOptions.cs
│   │       └── ParameterOptions.cs
│   ├── ElementOperation/      # 元素操作功能模块
│   │   ├── OperateElementCommand.cs
│   │   ├── OperateElementEventHandler.cs
│   │   └── Models/           # 元素操作模型
│   │       └── OperationSetting.cs
│   ├── UnifiedCommands/      # 统一命令功能模块（取代旧的族和系统族模块）
│   │   ├── CreateElementCommand.cs
│   │   ├── CreateElementEventHandler.cs
│   │   ├── GetElementCreationSuggestionCommand.cs
│   │   ├── GetElementCreationSuggestionEventHandler.cs
│   │   ├── Models/           # 统一创建模型
│   │   │   ├── ElementCreationParameters.cs
│   │   │   ├── ElementSuggestionParameters.cs
│   │   │   ├── FamilyCreationOptions.cs
│   │   │   ├── SystemCreationOptions.cs
│   │   │   ├── SystemElementParameters.cs
│   │   │   ├── WallSpecificParameters.cs
│   │   │   └── FloorSpecificParameters.cs
│   │   └── Utils/           # 统一工具类
│   │       └── ElementUtilityService.cs
│   └── RevitStatus/          # Revit状态功能模块
│       ├── GetRevitStatusCommand.cs
│       ├── GetRevitStatusEventHandler.cs
│       └── Models/           # 状态模型
│           └── RevitStatusInfo.cs
├── Models/                    # 数据模型层
│   ├── Common/               # 通用模型
│   │   ├── AIResult.cs
│   │   ├── CreationRequirements.cs
│   │   └── ParameterInfo.cs
│   └── Geometry/             # 几何模型
│       ├── JZPoint.cs
│       ├── JZLine.cs
│       └── JZFace.cs
├── Utils/                     # 工具类层
│   ├── FamilyCreation/       # 族创建工具类
│   │   └── FamilyInstanceCreator.cs
│   └── SystemCreation/       # 系统族创建工具类
│       ├── SystemElementCreator.cs
│       └── SystemElementValidator.cs
└── RevitMCPCommandSet.csproj  # 项目配置
```

## 开发规范

### 1. 命令实现标准

每个 MCP 命令需要实现两个核心类：

#### 数据格式规范
**强制要求**: 所有命令入口层接受的参数必须被 `"data"` 包裹，以保持接口的规整性和一致性。

**标准格式**：
```json
{
  "data": {
    // 实际的业务参数
    "param1": "value1",
    "param2": "value2"
  }
}
```

#### Command 类（继承 ExternalEventCommandBase）
```csharp
public class YourCommand : ExternalEventCommandBase
{
    public override string CommandName => "your_command_name";

    public YourCommand(UIApplication uiApp)
        : base(new YourEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        // 1. 强制解析 data 包裹层
        var dataToken = parameters["data"];
        if (dataToken == null)
        {
            return new AIResult<object>
            {
                Success = false,
                Message = "参数格式错误：缺少 'data' 包裹层"
            };
        }

        // 2. 解析实际业务参数
        var actualData = dataToken.ToObject<YourDataModel>();

        // 3. 设置 Handler 参数
        // 4. 触发异步事件
        // 5. 返回结果
    }
}
```

#### EventHandler 类（实现双接口）
```csharp
public class YourEventHandler : IExternalEventHandler, IWaitableExternalEventHandler
{
    private readonly ManualResetEvent _resetEvent = new ManualResetEvent(false);

    public void Execute(UIApplication uiapp)
    {
        try
        {
            // Revit API 操作
        }
        finally
        {
            _resetEvent.Set(); // 必须：通知完成
        }
    }

    public bool WaitForCompletion(int timeoutMilliseconds = 10000)
    {
        return _resetEvent.WaitOne(timeoutMilliseconds);
    }
}
```

### 2. 数据模型设计

#### JsonProperty 数据结构一致性规范 🔴 **重要**
**强制要求**：Revit 端 JsonProperty 属性名必须与服务端 Zod schema 完全一致

```csharp
// JsonProperty 属性名必须与服务端 Zod schema 完全一致
public class FloorSpecificParameters
{
    [JsonProperty("boundary")]  // 正确：与服务端 Zod schema 一致
    public List<JZPoint> Boundary { get; set; }
}
```

#### JsonProperty 使用规范
1. **属性命名**：JsonProperty 值必须使用 camelCase（如：`"elementId"`、`"locationPoint"`）
2. **命名一致性**：JsonProperty 值与服务端 Zod schema 属性名完全匹配
3. **嵌套对象**：复杂对象的所有层级都要保持命名一致
4. **数组元素**：数组元素类型的 JsonProperty 也要匹配

```csharp
// 标准 JsonProperty 示例
public class WallSpecificParameters
{
    [JsonProperty("line")]
    public JZLine Line { get; set; }

    [JsonProperty("height")]
    public double Height { get; set; }

    [JsonProperty("offset")]
    public double BaseOffset { get; set; } = 0;
}

// 对应服务端 Zod schema
wallParameters: z.object({
  line: z.object({...}),
  height: z.number(),
  offset: z.number().default(0)
})
```

#### 与服务端同步检查清单
修改数据模型时必须检查：
- [ ] JsonProperty 属性名与服务端 Zod schema 一致
- [ ] 嵌套对象的所有层级属性名都匹配
- [ ] 数组元素的属性结构完全对应
- [ ] 可选属性在两端声明一致
- [ ] 默认值设置保持同步

#### 统一返回格式
```csharp
public class AIResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Response { get; set; }
}
```

#### 坐标系统约定
- **单位**: 毫米 (mm) - 所有坐标和距离
- **转换**: Revit 内部单位 × 304.8 = 毫米
- **几何类**: 使用 JZPoint、JZLine、JZFace 等自定义类型

## 节点规范 v2.0 🔴 **重要更新**

ElementFilter模块采用全新的**节点化数据架构**，统一组织元素信息，提升AI理解和处理效率：

### 数据节点定义

| 节点名 | 内容 | 说明 | 适用元素 |
|--------|------|------|----------|
| `identity` | name, category, builtInCategory | 元素身份标识 | 所有元素 |
| `type` | typeId, typeName, familyId*, familyName* | **统一类型节点** | 所有元素 |
| `geometry` | location, boundingBox, thickness, height, area, profile | **统一几何节点** | 所有元素 |
| `level` | levelId, levelName | 所属标高 | 大部分元素 |
| `parameters` | instance, type 参数分类 | 元素参数 | 可选 |

**注**：`*` 标记字段仅族实例包含，系统族元素不包含。

### Type节点统一设计

**重要变更**：`type` 节点现在统一管理类型和族信息

```json
// 族实例（如门）
"type": {
  "typeId": 94654,
  "typeName": "750 x 2000mm",
  "familyId": 242453,        // 族实例专有
  "familyName": "单扇 - 与墙齐"  // 族实例专有
}

// 系统族（如墙）
"type": {
  "typeId": 398,
  "typeName": "常规 - 200mm"
  // 无 familyId/familyName
}
```

### Geometry节点统一设计

所有几何相关信息统一在 `geometry` 节点下：

```json
"geometry": {
  "location": {              // 统一位置字段（自动检测点/线）
    "point": {...}           // 或 "line": {...}
  },
  "boundingBox": {...},      // 包围盒
  "thickness": 200,          // 厚度（从根级别迁移）
  "height": 8000,            // 高度（从根级别迁移）
  "area": 53.18,             // 面积（从根级别迁移）
  "profile": [...]           // 轮廓（楼板等）
}
```

### Parameters节点分类

参数信息按类型分类组织：

```json
"parameters": {
  "instance": {              // 实例参数
    "高度": 3000,
    "宽度": 750
  },
  "type": {                  // 类型参数
    "默认高度": 2000,
    "功能": 1
  }
}
```

## 核心功能模块

### 1. AI 元素过滤器 (ai_element_filter) - 节点化v2.0
- **功能**: 智能查询和筛选 Revit 元素
- **支持**: 类别、类型、空间范围、可见性等多维度过滤
- **架构**: 节点化数据返回，统一的字段查询系统
- **返回**: 详细的元素信息（几何、参数、属性等）

### 2. 统一元素创建 (create_element)
- **功能**: 统一的元素创建命令，支持族实例和系统族元素
- **族实例支持**: 8种族放置类型（OneLevelBased、WorkPlaneBased、TwoLevelsBased、CurveBased、ViewBased等）
- **系统族支持**: Wall（墙体）、Floor（楼板），预留 Ceiling、Roof
- **智能化**: 自动类型检测、自动查找标高、自动搜索宿主、智能参数验证
- **适用范围**: 门、窗、设备、结构构件、墙体、楼板等所有Revit元素类型
- **架构特色**: 单一入口、统一参数模型、智能路由到具体创建器

### 3. 统一创建参数建议 (get_element_creation_suggestion)
- **功能**: 为AI提供统一的元素创建参数要求和指导
- **族实例分析**: 族放置类型、必需参数、可选参数、参数格式示例
- **系统族分析**: 必需参数、可选参数、参数格式示例、可用类型列表
- **智能检测**: 根据ElementId自动检测元素类型并提供相应建议
- **作用**: 统一AI对所有Revit元素创建需求的理解，提高创建成功率

### 4. 元素操作器 (operate_element)
- **功能**: 对元素进行各种操作
- **操作类型**: 选择、着色、透明度、隐藏、删除、隔离等
- **可视化**: 支持颜色标记和3D剖切框

## 快速开发指南

### 编译配置
- **标准编译配置**: Debug R20, x64
- **MSBuild路径**: `"D:\JetBrains\JetBrains Rider 2025.1.4\tools\MSBuild\Current\Bin\MSBuild.exe"`

### 编译命令
```bash
# 标准编译命令（推荐）
"D:\JetBrains\JetBrains Rider 2025.1.4\tools\MSBuild\Current\Bin\MSBuild.exe" "E:\工作文档\开发类\MyCode\Revit-MCP\revit-mcp-commandset\revit-mcp-commandset\RevitMCPCommandSet.csproj" -p:Configuration="Debug R20" -nologo -clp:ErrorsOnly
```

### 添加新功能模块

1. **创建功能模块目录**
   ```bash
   Features/YourNewFeature/
   ```

2. **创建 Command 和 EventHandler 类**
   ```bash
   Features/YourNewFeature/YourNewCommand.cs
   Features/YourNewFeature/YourNewEventHandler.cs
   ```

3. **更新命名空间**
   ```csharp
   namespace RevitMCPCommandSet.Features.YourNewFeature
   ```

4. **创建数据模型（如需要）**
   ```bash
   Features/YourNewFeature/Models/YourDataModel.cs
   ```

5. **更新 command.json**
   ```json
   {
     "commandName": "your_new_command",
     "description": "Your command description",
     "assemblyPath": "RevitMCPCommandSet.dll"
   }
   ```

### 功能模块组织原则

每个 Features 子目录代表一个完整的功能模块：
- **ElementFilter**: 元素查询和过滤相关功能
- **UnifiedCommands**: 统一元素创建和参数建议功能（整合原FamilyInstanceCreation和SystemElementCreation）
- **ElementOperation**: 元素操作相关功能
- **RevitStatus**: Revit状态查询功能

### 命名空间规范

- 功能模块命名空间：`RevitMCPCommandSet.Features.{ModuleName}`
- 模块模型命名空间：`RevitMCPCommandSet.Features.{ModuleName}.Models`
- 公共模型命名空间：`RevitMCPCommandSet.Models.Common`
- 几何模型命名空间：`RevitMCPCommandSet.Models.Geometry`
- 工具类命名空间：`RevitMCPCommandSet.Utils`

## 注意事项

1. **数据格式规范**: 所有命令必须强制要求参数被 `"data"` 包裹，确保接口一致性
2. **线程安全**: 所有 Revit API 调用必须在主线程执行
3. **事务管理**: 修改操作需要包装在 Transaction 中
4. **资源释放**: 适当释放 ManualResetEvent 等资源
5. **命名一致性**: 确保与 revit-mcp 服务端命令名称一致
6. **单位转换**: 注意 Revit 内部单位与毫米的转换，使用比例304.8进行换算
7. **模块独立性**: 各功能模块应保持相对独立，减少耦合

## 常见问题

**Q: 如何处理 Revit 版本兼容性？**
A: 使用条件编译指令 `#if REVIT2023_OR_GREATER` 等。

**Q: 参数解析失败怎么办？**
A: 首先检查是否有 "data" 包裹层，然后检查 JSON 结构是否与数据模型匹配，使用 try-catch 捕获解析异常。

**Q: 数据结构不一致导致参数反序列化失败怎么办？** 🔴 **重要**
A: 这是最常见的集成问题，按以下步骤排查：
1. **检查 JsonProperty**：确认 `[JsonProperty("属性名")]` 与服务端 Zod schema 属性名完全一致
2. **验证嵌套结构**：复杂对象的所有层级 JsonProperty 都要匹配
3. **测试反序列化**：在 EventHandler 中打印接收到的 JSON 和反序列化后的对象
4. **对比定义**：对比服务端 `src/tools/*.ts` 和本端 `Features/*/Models/*.cs` 的参数定义
5. **检查属性类型**：确保 C# 属性类型与 TypeScript 类型兼容

**Q: 如何避免数据结构不一致问题？**
A: 遵循以下开发规范：
1. **修改前核对**：修改 JsonProperty 前，先查看服务端对应的 Zod schema
2. **统一命名约定**：严格使用 camelCase 作为 JsonProperty 值
3. **同步修改**：任一端修改数据结构时，同步更新另一端
4. **定期验证**：定期进行端到端测试验证数据传输正确性
5. **文档更新**：结构变更后及时更新相关文档

---

更多详细信息请参考项目源码和 RevitMCPSDK 文档。

### 📋 相关文档链接
- [元素过滤器文档](./revit-mcp-commandset/Features/ElementFilter/README.md)
- [统一命令功能文档](./revit-mcp-commandset/Features/UnifiedCommands/README.md)
