# Revit MCP CommandSet 项目架构文档

## 项目概述

本项目是 Revit MCP 生态系统的核心组件，负责在 Revit 端实现与 AI 助手的通信桥梁。通过统一的命令模式，为 LLM 提供访问和操作 Revit 模型的能力。

### 核心特性
- 🔗 **AI-BIM 连接**: 连接大语言模型与 Revit 软件的桥梁
- 🏗️ **统一架构**: 基于 RevitMCPSDK 的标准化开发模式
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
├── Commands/                  # 命令入口层
│   ├── AIElementFilterCommand.cs
│   ├── CreatePointElementCommand.cs
│   └── OperateElementCommand.cs
├── Services/                  # Revit功能实现层
│   ├── AIElementFilterEventHandler.cs
│   ├── CreatePointElementEventHandler.cs
│   └── OperateElementEventHandler.cs
├── Models/                    # 数据模型层
│   ├── Common/               # 通用模型
│   │   ├── AIResult.cs
│   │   ├── FilterSetting.cs
│   │   └── OperationSetting.cs
│   └── Geometry/             # 几何模型
│       ├── JZPoint.cs
│       ├── JZLine.cs
│       └── JZFace.cs
├── Utils/                     # 工具类层
└── RevitMCPCommandSet.csproj  # 项目配置
```

## 开发规范

### 1. 命令实现标准

每个 MCP 命令需要实现两个核心类：

#### Command 类（继承 ExternalEventCommandBase）
```csharp
public class YourCommand : ExternalEventCommandBase
{
    public override string CommandName => "your_command_name";

    public YourCommand(UIApplication uiApp)
        : base(new YourEventHandler(), uiApp) { }

    public override object Execute(JObject parameters, string requestId)
    {
        // 1. 参数解析
        // 2. 设置 Handler 参数
        // 3. 触发异步事件
        // 4. 返回结果
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

## 核心功能模块

### 1. AI 元素过滤器 (ai_element_filter)
- **功能**: 智能查询和筛选 Revit 元素
- **支持**: 类别、类型、空间范围、可见性等多维度过滤
- **返回**: 详细的元素信息（几何、参数、属性等）

### 2. 点状元素创建 (create_point_based_element)
- **功能**: 创建基于点定位的族实例
- **支持**: 门、窗、设备等点状构件
- **参数**: 位置、尺寸、族类型、标高等

### 3. 元素操作器 (operate_element)
- **功能**: 对元素进行各种操作
- **操作类型**: 选择、着色、透明度、隐藏、删除、隔离等
- **可视化**: 支持颜色标记和3D剖切框
