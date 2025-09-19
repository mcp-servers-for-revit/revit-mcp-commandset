# FamilyInstanceCreation 功能模块

## 概述

FamilyInstanceCreation 是 Revit MCP CommandSet 的核心功能模块，基于强大的 `FamilyInstanceCreator` 工具类，为 AI 助手提供智能的族实例创建能力。该模块通过两个核心 MCP 命令，实现了从参数分析到实例创建的完整工作流程。

### 核心价值

- 🤖 **AI 友好**：结构化的参数需求分析，帮助 AI 理解和生成正确的创建参数
- 🏗️ **全面支持**：覆盖 8 种主要的 Revit 族放置类型（除 Adaptive 外）
- ⚡ **自动化**：智能的标高查找、宿主元素搜索和单位转换
- 🛡️ **安全可靠**：完整的事务管理、错误处理和线程安全设计

## MCP 命令

### 1. get_family_creation_suggestion

获取指定族类型的创建参数需求和建议。

#### 请求格式

```json
{
  "typeId": 12345
}
```

#### 参数说明

| 参数 | 类型 | 必需 | 说明 |
|------|------|------|------|
| typeId | int | ✅ | 族类型的 ElementId |

#### 响应格式

```json
{
  "success": true,
  "message": "成功获取族创建参数需求",
  "response": {
    "typeId": 12345,
    "familyName": "公制常规模型",
    "typeName": "桌子",
    "placementType": "OneLevelBased",
    "isSupported": true,
    "requiredParameters": {
      "locationPoint": {
        "type": "JZPoint",
        "unit": "mm",
        "description": "放置点坐标",
        "example": { "x": 5000.0, "y": 3000.0, "z": 0.0 }
      }
    },
    "optionalParameters": {
      "baseLevelId": {
        "type": "int",
        "unit": "ElementId",
        "description": "关联标高的ElementId，不指定时自动查找最近标高",
        "example": 12345
      },
      "baseOffset": {
        "type": "double",
        "unit": "mm",
        "description": "相对标高的偏移距离",
        "example": 1000.0
      }
    },
    "examples": {
      "typical": {
        "typeId": 12345,
        "locationPoint": { "x": 5000.0, "y": 3000.0, "z": 0.0 },
        "autoFindLevel": true,
        "baseOffset": 500.0
      }
    }
  }
}
```

### 2. create_family_instance

根据指定参数创建族实例。

#### 请求格式

```json
{
  "data": {
    "typeId": 12345,
    "locationPoint": { "x": 5000.0, "y": 3000.0, "z": 0.0 },
    "autoFindLevel": true,
    "baseOffset": 500.0
  }
}
```

#### 参数说明

核心参数：

| 参数 | 类型 | 必需 | 说明 |
|------|------|------|------|
| typeId | int | ✅ | 族类型的 ElementId |
| locationPoint | JZPoint | 部分必需 | 放置点坐标（毫米） |
| locationLine | JZLine | 部分必需 | 放置线段（毫米） |
| baseLevelId | int | 部分必需 | 底部标高 ElementId |
| topLevelId | int | 可选 | 顶部标高 ElementId |
| viewId | int | 部分必需 | 视图 ElementId |

控制参数：

| 参数 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| autoFindLevel | bool | true | 是否自动查找最近标高 |
| autoFindHost | bool | true | 是否自动查找宿主元素 |
| searchRadius | double | 1000 | 搜索半径（毫米） |
| baseOffset | double | 0 | 底部偏移（毫米） |
| topOffset | double | 0 | 顶部偏移（毫米） |
| hostCategories | string[] | null | 宿主类别名称数组 |
| faceDirection | JZPoint | null | 面方向向量（归一化） |
| handDirection | JZPoint | null | 手方向向量（归一化） |

#### 响应格式

```json
{
  "success": true,
  "message": "族实例创建成功",
  "response": {
    "success": true,
    "message": "族实例创建成功",
    "elementId": 67890,
    "elementType": "专用设备",
    "additionalInfo": {
      "FamilyName": "公制常规模型",
      "TypeName": "桌子",
      "PlacementType": "OneLevelBased"
    }
  }
}
```

## 支持的族类型

### 1. OneLevelBased（基于单个标高）

适用于：公制常规模型、家具等

**必需参数**：
- `locationPoint`: 放置点坐标

**可选参数**：
- `baseLevelId`: 关联标高
- `baseOffset`: 标高偏移
- `autoFindLevel`: 自动查找标高

**示例**：
```json
{
  "typeId": 12345,
  "locationPoint": { "x": 5000, "y": 3000, "z": 0 },
  "autoFindLevel": true,
  "baseOffset": 500
}
```

### 2. OneLevelBasedHosted（基于单个标高和宿主）

适用于：门、窗、嵌入式设备等

**必需参数**：
- `locationPoint`: 放置点坐标（将投影到宿主上）

**可选参数**：
- `hostElementId`: 手动指定宿主元素
- `baseLevelId`: 关联标高
- `autoFindHost`: 自动查找宿主
- `searchRadius`: 宿主搜索半径

**示例**：
```json
{
  "typeId": 54321,
  "locationPoint": { "x": 5000, "y": 2000, "z": 1000 },
  "autoFindHost": true,
  "searchRadius": 5000
}
```

### 3. TwoLevelsBased（基于两个标高）

适用于：柱子、竖向构件等

**必需参数**：
- `locationPoint`: 柱子底部定位点
- `baseLevelId`: 底部标高

**可选参数**：
- `topLevelId`: 顶部标高
- `baseOffset`: 底部偏移
- `topOffset`: 顶部偏移

**示例**：
```json
{
  "typeId": 98765,
  "locationPoint": { "x": 2000, "y": 1000, "z": 0 },
  "baseLevelId": 12345,
  "topLevelId": 12346,
  "baseOffset": 0,
  "topOffset": 0
}
```

### 4. WorkPlaneBased（基于工作平面）

适用于：基于面的公制常规模型、设备等

**必需参数**：
- `locationPoint`: 在工作平面上的放置点

**可选参数**：
- `faceDirection`: 面方向向量
- `handDirection`: 手方向向量
- `hostCategories`: 宿主类别数组
- `autoFindHost`: 自动查找宿主面
- `searchRadius`: 搜索半径

**示例**：
```json
{
  "typeId": 13579,
  "locationPoint": { "x": 3000, "y": 4000, "z": 1200 },
  "autoFindHost": true,
  "hostCategories": ["OST_Floors", "OST_Walls"]
}
```

### 5. CurveBased（基于线）

适用于：基于线的公制常规模型、扶手等

**必需参数**：
- `locationLine`: 基准线段

**可选参数**：
- `baseLevelId`: 关联标高
- `hostCategories`: 宿主面类别
- `autoFindLevel`: 自动查找最近标高

**示例**：
```json
{
  "typeId": 24680,
  "locationLine": {
    "p0": { "x": 0, "y": 0, "z": 0 },
    "p1": { "x": 5000, "y": 0, "z": 0 }
  },
  "autoFindLevel": true
}
```

### 6. ViewBased（基于视图）

适用于：详图注释、标记等

**必需参数**：
- `locationPoint`: 视图中的放置点（Z值忽略）
- `viewId`: 目标2D视图

**示例**：
```json
{
  "typeId": 11111,
  "locationPoint": { "x": 2000, "y": 1500, "z": 0 },
  "viewId": 67890
}
```

### 7. CurveBasedDetail（基于线的详图）

适用于：详图组件等

**必需参数**：
- `locationLine`: 详图线段（在视图平面内）
- `viewId`: 目标2D视图

**示例**：
```json
{
  "typeId": 22222,
  "locationLine": {
    "p0": { "x": 1000, "y": 500, "z": 0 },
    "p1": { "x": 3000, "y": 500, "z": 0 }
  },
  "viewId": 67890
}
```

### 8. CurveDrivenStructural（结构曲线驱动）

适用于：梁、支撑、斜柱等

**必需参数**：
- `locationLine`: 结构轴线
- `baseLevelId`: 参照标高

**示例**：
```json
{
  "typeId": 33333,
  "locationLine": {
    "p0": { "x": 0, "y": 0, "z": 3000 },
    "p1": { "x": 8000, "y": 0, "z": 3000 }
  },
  "baseLevelId": 12345
}
```

## 技术细节

### 单位转换

- **输入单位**：所有长度坐标使用**毫米（mm）**
- **内部转换**：自动转换为 Revit 内部单位（英尺）
- **转换比例**：1 英尺 = 304.8 毫米

### 自动查找机制

#### 标高查找
```csharp
// 当 autoFindLevel = true 且未指定 baseLevelId 时
// 系统会查找距离 locationPoint.Z 最近的标高
Level nearestLevel = FamilyInstanceCreator.GetNearestLevel(doc, locationPoint.Z / 304.8);
```

#### 宿主查找
```csharp
// 当 autoFindHost = true 时
// 系统会在 searchRadius 范围内查找合适的宿主元素
// 支持墙、楼板、天花板、屋顶等类别
```

### 宿主类别

支持的 BuiltInCategory 名称：

- `OST_Walls` - 墙
- `OST_Floors` - 楼板
- `OST_Ceilings` - 天花板
- `OST_Roofs` - 屋顶
- `OST_GenericModel` - 常规模型
- `OST_StructuralFraming` - 结构框架
- `OST_StructuralColumns` - 结构柱

### 方向向量

对于 WorkPlaneBased 族：

- **faceDirection**：面方向向量，通常指向法线方向
- **handDirection**：手方向向量，定义族的X轴方向
- 所有方向向量应为**归一化向量**（长度为1）

## 最佳实践

### 1. 参数获取建议流程

```
1. 调用 get_family_creation_suggestion 获取参数需求
2. 根据返回的 requiredParameters 和 optionalParameters 构造请求
3. 参考 examples 中的典型示例
4. 调用 create_family_instance 创建实例
```

### 2. 错误处理

// 错误响应示例
```json
{
  "success": false,
  "message": "OneLevelBased族必须指定locationPoint",
  "response": {
    "success": false,
    "message": "OneLevelBased族必须指定locationPoint",
    "elementId": -1
  }
}
```

### 3. 性能优化

- 优先使用 `autoFindLevel` 和 `autoFindHost` 而非手动查找
- 合理设置 `searchRadius` 避免过大的搜索范围
- 对于重复创建，缓存标高和视图的 ElementId

## 注意事项

### 限制条件

1. **不支持 Adaptive 族**：自适应族需要多个自适应点，当前版本暂不支持
2. **线程安全**：所有 Revit API 调用在主线程执行，通过 ExternalEvent 机制保证安全
3. **事务管理**：创建操作自动包装在事务中，失败时自动回滚

### 坐标系统

- 所有坐标使用 Revit 项目坐标系
- Z 轴正方向向上
- 单位为毫米（mm）

### 族类型要求

- 族类型必须已加载到项目中
- 族类型会自动激活（`FamilySymbol.Activate()`）
- 无效的 ElementId 会返回错误

## 故障排除

### 常见错误

#### 1. "找不到族类型"
**原因**：ElementId 无效或族类型未加载
**解决**：检查 typeId 是否正确，确保族已加载到项目中

#### 2. "找不到合规的宿主信息"
**原因**：自动查找宿主失败
**解决**：
- 增大 searchRadius
- 手动指定 hostElementId
- 检查 hostCategories 设置

#### 3. "创建失败，可能是宿主条件不满足"
**原因**：宿主元素不支持该族类型
**解决**：
- 检查族的宿主行为设置
- 确认宿主元素类型正确
- 调整放置点位置

#### 4. "视图族必须指定有效的viewId"
**原因**：ViewBased 或 CurveBasedDetail 族缺少视图参数
**解决**：提供有效的 2D 视图 ElementId

### 调试技巧

1. **启用日志**：检查 `System.Diagnostics.Trace` 输出
2. **参数验证**：先调用 suggestion 命令验证参数需求
3. **逐步测试**：从简单的 OneLevelBased 族开始测试

## 版本兼容性

- **支持版本**：Revit 2020-2025
- **推荐版本**：Revit 2024+
- **依赖项**：RevitMCPSDK、Newtonsoft.Json

## 更新日志

### v1.0.0
- 初始版本
- 支持 8 种族放置类型
- 完整的参数分析和创建功能
- 自动化标高和宿主查找