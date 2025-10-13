# RevitStatus 模块

## 模块概述

RevitStatus 模块提供 Revit 应用程序和当前项目的状态查询功能，允许 AI 助手获取 Revit 环境的实时信息。

## 功能说明

### 主要功能

- **应用程序信息**：Revit 版本、语言、用户名
- **文档状态**：当前活动文档、文档路径、是否已保存
- **视图信息**：当前活动视图、视图类型、视图名称
- **项目信息**：项目名称、项目编号、项目地址
- **系统信息**：.NET 版本、系统架构

### API 命令

**命令名**：`get_revit_status`

**调用方式**：
```json
{
  "data": {}
}
```

注意：此命令不需要任何参数，`data` 对象为空即可。

## 返回格式

### 成功响应

```json
{
  "Success": true,
  "Message": "成功获取 Revit 状态信息",
  "Response": {
    "Application": {
      "Version": "2024",
      "SubVersion": "2024.0.1",
      "Language": "Chinese_Simplified",
      "Username": "用户名"
    },
    "Document": {
      "IsActive": true,
      "Title": "项目名称",
      "PathName": "C:\\Projects\\MyProject.rvt",
      "IsSaved": true,
      "IsDetached": false,
      "IsWorkshared": false
    },
    "ActiveView": {
      "Name": "楼层平面: 标高 1",
      "ViewType": "FloorPlan",
      "Scale": 100,
      "DetailLevel": "Medium"
    },
    "Project": {
      "Name": "示例项目",
      "Number": "2024-001",
      "Address": "项目地址",
      "Author": "设计师"
    },
    "System": {
      "DotNetVersion": "4.8",
      "Is64Bit": true,
      "ProcessorCount": 8,
      "TotalMemory": "16384 MB"
    }
  }
}
```

### 错误响应

```json
{
  "Success": false,
  "Message": "无活动文档",
  "Response": null
}
```

## 数据模型

### RevitStatusInfo

主要状态信息容器：

```csharp
public class RevitStatusInfo
{
    public ApplicationInfo Application { get; set; }
    public DocumentInfo Document { get; set; }
    public ViewInfo ActiveView { get; set; }
    public ProjectInfo Project { get; set; }
    public SystemInfo System { get; set; }
}
```

### ApplicationInfo

Revit 应用程序信息：

| 字段 | 类型 | 说明 |
|-----|------|------|
| Version | string | Revit 主版本号 |
| SubVersion | string | 完整版本号 |
| Language | string | 界面语言 |
| Username | string | 当前用户名 |

### DocumentInfo

活动文档信息：

| 字段 | 类型 | 说明 |
|-----|------|------|
| IsActive | bool | 是否有活动文档 |
| Title | string | 文档标题 |
| PathName | string | 文件路径 |
| IsSaved | bool | 是否已保存 |
| IsDetached | bool | 是否分离 |
| IsWorkshared | bool | 是否工作共享 |

### ViewInfo

当前视图信息：

| 字段 | 类型 | 说明 |
|-----|------|------|
| Name | string | 视图名称 |
| ViewType | string | 视图类型 |
| Scale | int | 视图比例 |
| DetailLevel | string | 详细程度 |

## 使用场景

### 场景1：环境检查

AI 助手在执行操作前检查环境：
```json
// 请求
{
  "data": {}
}

// 用途：确认 Revit 版本是否支持某些功能
```

### 场景2：文档验证

验证是否有活动文档：
```json
// 响应检查
if (response.Response.Document.IsActive) {
    // 可以执行文档相关操作
}
```

### 场景3：视图类型判断

根据当前视图类型决定操作：
```json
// 检查是否在平面视图
if (response.Response.ActiveView.ViewType === "FloorPlan") {
    // 执行平面相关操作
}
```

## 注意事项

### 性能考虑

- 状态查询是轻量级操作，响应迅速
- 建议在会话开始时查询一次，缓存结果
- 文档切换时需要重新查询

### 错误处理

常见错误情况：
1. **无活动文档**：Revit 未打开任何项目
2. **无活动视图**：文档存在但无活动视图
3. **权限问题**：某些信息可能因权限无法获取

### 语言支持

`Language` 字段可能的值：
- `English`
- `Chinese_Simplified`
- `Chinese_Traditional`
- `Japanese`
- `German`
- `French`
- 其他 Revit 支持的语言

## 实现文件

- **命令类**：`GetRevitStatusCommand.cs`
- **事件处理**：`GetRevitStatusEventHandler.cs`
- **数据模型**：`Models/RevitStatusInfo.cs`

## 扩展建议

未来可能的扩展：
1. 添加插件信息查询
2. 添加性能指标监控
3. 添加许可证状态
4. 添加最近使用的命令历史
5. 添加当前选择集信息

## 相关文档

- **主文档**：[CLAUDE.md](../../CLAUDE.md)
- **测试文档**：[Test/README.md](../../Test/README.md)
- **API 规范**：RevitMCPSDK 文档