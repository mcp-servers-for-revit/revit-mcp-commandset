# AIElementFilter 优化实施方案
*版本: 1.0 | 日期: 2025-09-24*

## 一、优化背景与目标

### 背景
当前AIElementFilter功能采用"一刀切"模式，无论查询需求如何都返回完整的元素信息，导致：
- 数据传输量大，影响性能
- AI难以精准控制所需信息
- 无法支持已知ElementId的快速查询

### 目标
实现信息粒度精准控制，满足不同场景的查询需求：
1. 支持分层信息返回（Minimal → Full）
2. 支持ElementId列表直接查询
3. 增强参数查询能力
4. 保持向后兼容

## 二、ReturnLevel 字段清单对照表

| ReturnLevel | 必返字段 | 可选字段 | 数据量 | 适用场景 |
|------------|---------|----------|--------|---------|
| **Minimal** (默认) | • elementId<br>• uniqueId | - | 最小 | 仅需要元素标识 |
| **Basic** | • elementId<br>• uniqueId<br>• name<br>• category<br>• builtInCategory<br>• familyId<br>• typeId<br>• levelId<br>• documentGuid | • levelName<br>• typeName<br>• familyName | 轻量 | 元素识别、分类统计 |
| **Geometry** | 包含Basic所有字段<br>+<br>• boundingBox<br>• transform | **FamilyInstance:**<br>• locationPoint<br>• rotation<br>• handOrientation*<br>• facingOrientation*<br>• isHandFlipped*<br>• isFacingFlipped*<br><br>**线性元素:**<br>• locationCurve<br>• curveDirection<br>• length<br><br>**面元素:**<br>• hostLevelId<br>• slope<br>• thickness<br>• area<br>• perimeter | 中等 | 空间分析、定位计算 |
| **Parameters** | 包含Basic所有字段<br>+<br>• instanceParameters[]<br>• typeParameters[] | 每个参数包含:<br>• name<br>• displayValue<br>• rawValue<br>• storageType<br>• builtInName<br>• builtInEnum<br>• isReadOnly<br>• guid<br>• source | 较大 | 属性查询、参数分析 |
| **Full** | Basic + Geometry + Parameters<br>+<br>• viewStates[]<br>• ownerViewId<br>• worksetId<br>• designOptionId<br>• phaseCreated<br>• phaseDemolished | • materials[]<br>• hostElementId<br>• connectedElements[] | 最大 | 完整分析、数据导出 |
| **Custom** | 根据includeFields指定 | - | 可控 | 精准字段选择 |

*注：带*号字段为高开销项，需显式启用（通过GeometryOptions配置）

## 三、ElementIds 过滤流程设计

### 3.1 字段定义
```json
{
  "data": {
    "elementIds": [123456, 789012],  // 精准元素ID列表
    // 其他过滤条件...
  }
}
```

### 3.2 过滤逻辑流程图
```
输入
  ↓
[elementIds存在?]
  ├─是→ [其他过滤条件存在?]
  │      ├─是→ 场景2: 先获取ID对应元素 → 应用其他过滤 → 返回交集
  │      └─否→ 场景1: 直接获取ID对应元素 → 返回
  └─否→ 场景3: 传统过滤流程（现有逻辑）
```

### 3.3 缺失元素处理策略
```csharp
// 返回示例
{
  "Success": true,
  "Message": "查询成功。请求10个元素，找到8个，2个不存在（ID: 999999, 888888）",
  "Response": [/* 8个元素的信息 */]
}
```

### 3.4 与其他过滤条件的交互规则
- **单独使用elementIds**：跳过所有其他过滤条件，直接返回指定元素
- **elementIds + 其他条件**：先获取ID列表对应元素，再应用其他过滤条件（AND逻辑）
- **空elementIds**：忽略此字段，使用其他过滤条件

## 四、ParameterOptions 详细定义

### 4.1 配置结构
```csharp
public class ParameterOptions
{
    [JsonProperty("scope")]
    public string Scope { get; set; } = "Instance";  // 默认：仅实例参数
    // 可选值: "Instance" | "Type" | "Both"

    [JsonProperty("filterMode")]
    public string FilterMode { get; set; } = "None";  // 默认：返回所有参数
    // 可选值: "None" | "Include" | "Exclude"

    [JsonProperty("parameterNames")]
    public List<string> ParameterNames { get; set; }  // 参数名列表

    [JsonProperty("builtInParameters")]
    public List<string> BuiltInParameters { get; set; }  // 内置参数名列表

    [JsonProperty("returnFormat")]
    public string ReturnFormat { get; set; } = "Merged";  // 默认：合并返回
    // 可选值: "Merged" | "Separated"
}
```

### 4.2 默认行为说明
| 配置项 | 默认值 | 行为描述 |
|--------|--------|----------|
| scope | "Instance" | 仅返回实例参数 |
| filterMode | "None" | 返回所有参数（不过滤） |
| returnFormat | "Merged" | 实例和类型参数合并在一个字典中 |
| parameterNames | null | 不根据名称过滤 |
| builtInParameters | null | 不根据内置参数过滤 |

### 4.3 返回格式示例

#### Merged格式（默认）
```json
{
  "elementId": 123456,
  "parameters": {
    "Height": "3000 mm",          // 实例参数
    "Type.Width": "200 mm",       // 类型参数（自动加Type.前缀）
    "CustomParam": "Value",
    "Type.Material": "Concrete"
  }
}
```

#### Separated格式
```json
{
  "elementId": 123456,
  "instanceParameters": [
    {
      "name": "Height",
      "displayValue": "3000 mm",
      "rawValue": 3000.0,
      "storageType": "Double",
      "builtInName": "INSTANCE_HEIGHT_PARAM",
      "builtInEnum": 2003400,
      "isReadOnly": false,
      "guid": "a4f3b2c1-...",
      "source": "Instance"
    }
  ],
  "typeParameters": [
    {
      "name": "Width",
      "displayValue": "200 mm",
      "rawValue": 200.0,
      "storageType": "Double",
      "builtInName": "WALL_ATTR_WIDTH_PARAM",
      "builtInEnum": 2003401,
      "isReadOnly": false,
      "guid": "b5f4c3d2-...",
      "source": "Type"
    }
  ]
}
```

### 4.4 FilterMode行为矩阵

| FilterMode | parameterNames | builtInParameters | 行为 |
|------------|---------------|-------------------|------|
| None | 忽略 | 忽略 | 返回所有参数 |
| Include | ["Height"] | ["WALL_HEIGHT_TYPE"] | 仅返回Height和WALL_HEIGHT_TYPE |
| Exclude | ["Password"] | ["LICENSE_KEY"] | 返回除了Password和LICENSE_KEY外的所有参数 |

## 五、现有方法复用策略

### 5.1 保留并复用的核心方法

| 方法名 | 当前用途 | 复用策略 |
|--------|----------|----------|
| **GetFilteredElements** | 元素过滤主逻辑 | 保留，增加elementIds分支 |
| **GetElementLevel** | 获取标高信息 | Basic级别直接调用 |
| **GetBoundingBoxInfo** | 获取包围盒 | Geometry级别核心方法 |
| **GetThicknessInfo** | 获取厚度参数 | Parameters级别调用 |
| **GetDimensionParameters** | 获取尺寸参数 | Parameters级别基础 |
| **IsDimensionParameter** | 判断参数类型 | 参数过滤时使用 |
| **CreateElementFullInfo** | 创建完整信息 | 改造为Full级别专用 |
| **CreateTypeFullInfo** | 创建类型信息 | 类型元素处理 |
| **CreatePositioningElementInfo** | 标高轴网信息 | 特殊元素处理 |
| **CreateSpatialElementInfo** | 房间区域信息 | 空间元素处理 |
| **CreateViewInfo** | 视图信息 | 视图元素处理 |
| **CreateAnnotationInfo** | 注释信息 | 注释元素处理 |
| **CreateGroupOrLinkInfo** | 组和链接信息 | 特殊元素处理 |

### 5.2 新增辅助方法

| 方法名 | 用途 | 调用时机 |
|--------|------|----------|
| **BuildElementInfo** | 信息装配主分发 | 替代GetElementFullInfo |
| **BuildMinimalInfo** | 构建最小信息 | Minimal级别 |
| **BuildBasicInfo** | 构建基础信息 | Basic级别 |
| **BuildGeometryInfo** | 构建几何信息 | Geometry级别 |
| **BuildParametersInfo** | 构建参数信息 | Parameters级别 |
| **ExtractParameters** | 提取参数集合 | Parameters级别核心 |
| **ShouldIncludeParameter** | 参数过滤判断 | 参数筛选 |
| **IsSensitiveParameter** | 敏感参数判断 | 安全脱敏 |

## 六、实施计划

### 第一阶段：基础架构（2天）
- [ ] 扩展FilterSetting模型
- [ ] 添加新的验证逻辑
- [ ] 创建DTO模型文件

### 第二阶段：过滤增强（1天）
- [ ] 实现elementIds直接查询
- [ ] 处理混合过滤逻辑
- [ ] 添加缺失元素提示

### 第三阶段：信息分层（3天）
- [ ] 实现BuildElementInfo分发器
- [ ] 完成5个级别的Build方法
- [ ] 集成现有辅助方法

### 第四阶段：参数处理（2天）
- [ ] 实现ExtractParameters
- [ ] 添加过滤模式支持
- [ ] 实现敏感参数脱敏

### 第五阶段：测试与文档（2天）
- [ ] 编写单元测试
- [ ] 更新README文档
- [ ] 添加使用示例

## 七、API使用示例

### 7.1 最轻量查询（仅获取ID）
```json
{
  "data": {
    "filterCategory": "OST_Walls",
    "returnLevel": "Minimal",
    "maxElements": 1000
  }
}
```

### 7.2 已知ID获取详细信息
```json
{
  "data": {
    "elementIds": [123456, 789012],
    "returnLevel": "Parameters",
    "parameterOptions": {
      "scope": "Both",
      "filterMode": "Include",
      "parameterNames": ["Height", "Width", "Material"]
    }
  }
}
```

### 7.3 空间范围内的几何查询
```json
{
  "data": {
    "filterCategory": "OST_Furniture",
    "boundingBoxMin": {"p0": {"x": 0, "y": 0, "z": 0}, "p1": {"x": 5000, "y": 5000, "z": 3000}},
    "boundingBoxMax": {"p0": {"x": 5000, "y": 5000, "z": 3000}, "p1": {"x": 10000, "y": 10000, "z": 6000}},
    "returnLevel": "Geometry"
  }
}
```

### 7.4 自定义字段选择
```json
{
  "data": {
    "filterCategory": "OST_Doors",
    "returnLevel": "Custom",
    "includeFields": [
      "elementId",
      "name",
      "level.name",
      "geometry.locationPoint",
      "parameters.FireRating"
    ]
  }
}
```

## 八、向后兼容性保证

1. **默认行为不变**：ReturnLevel默认为"Minimal"，保持轻量返回
2. **所有新字段可选**：不影响现有调用方式
3. **错误处理一致**：保持现有的AIResult返回格式
4. **日志输出保留**：Trace输出继续保留用于调试

## 九、性能优化建议

1. **使用合适的ReturnLevel**：根据实际需求选择，避免过度查询
2. **设置maxElements限制**：大量元素时使用分页
3. **启用filterVisibleInCurrentView**：在复杂模型中减少处理量
4. **谨慎使用Full级别**：仅在必要时使用完整信息
5. **利用elementIds直查**：已知ID时跳过过滤提升性能

## 十、安全与隐私

### 敏感参数列表（自动脱敏）
- Password
- License
- SerialNumber
- AuthToken
- ApiKey
- PrivateKey

### 脱敏返回示例
```json
{
  "name": "LicenseKey",
  "displayValue": "[REDACTED]",
  "rawValue": "[REDACTED]",
  "isReadOnly": true
}
```

## 附录A：数据结构定义

### FilterSetting完整定义
```csharp
public class FilterSetting
{
    // ===== 现有过滤字段 =====
    [JsonProperty("filterCategory")]
    public string FilterCategory { get; set; }

    [JsonProperty("filterElementType")]
    public string FilterElementType { get; set; }

    [JsonProperty("filterFamilySymbolId")]
    public int FilterFamilySymbolId { get; set; } = -1;

    [JsonProperty("includeTypes")]
    public bool IncludeTypes { get; set; } = false;

    [JsonProperty("includeInstances")]
    public bool IncludeInstances { get; set; } = true;

    [JsonProperty("filterVisibleInCurrentView")]
    public bool FilterVisibleInCurrentView { get; set; } = false;

    [JsonProperty("boundingBoxMin")]
    public JZPoint BoundingBoxMin { get; set; }

    [JsonProperty("boundingBoxMax")]
    public JZPoint BoundingBoxMax { get; set; }

    [JsonProperty("maxElements")]
    public int MaxElements { get; set; } = 50;

    // ===== 新增字段 =====
    [JsonProperty("elementIds")]
    public List<int> ElementIds { get; set; }

    [JsonProperty("returnLevel")]
    public string ReturnLevel { get; set; } = "Minimal";

    [JsonProperty("parameterOptions")]
    public ParameterOptions ParameterOptions { get; set; }

    [JsonProperty("includeFields")]
    public List<string> IncludeFields { get; set; }

    [JsonProperty("geometryOptions")]
    public GeometryOptions GeometryOptions { get; set; }
}
```

## 附录B：版本更新记录

| 版本 | 日期 | 更新内容 |
|------|------|----------|
| 1.0 | 2025-09-24 | 初始方案，包含完整的优化设计 |

---
*本文档为AIElementFilter功能优化的技术实施方案，用于指导开发实施。*