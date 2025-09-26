# ElementVisual 模块

## 概述

`operate_element_visual` 命令专门处理纯视觉效果操作，不改变模型的几何形状或属性。这是新架构中最轻量、最高频使用的命令。

## 支持的操作

| 操作 | 说明 | 参数要求 |
|------|------|---------|
| **Select** | 在Revit界面中选中指定元素 | elementIds |
| **SelectionBox** | 创建3D剖切框并聚焦元素 | elementIds |
| **Highlight** | 快捷高亮（红色标记） | elementIds |
| **SetColor** | 设置自定义颜色标记 | elementIds + colorValue |
| **SetTransparency** | 调整元素透明度 | elementIds + transparencyValue |

## 参数说明

### 必填参数

- **elementIds** (number[]): 目标元素ID数组，至少包含1个元素
- **visualAction** (string): 操作类型，必须是枚举值之一

### 可选参数

- **colorValue** (number[3]): RGB颜色值，范围0-255，默认[255,0,0]（红色）
- **transparencyValue** (number): 透明度，范围0-100，默认50

## 调用示例

### 1. 高亮元素（最简调用）
```json
{
  "data": {
    "elementIds": [12345, 67890],
    "visualAction": "Highlight"
  }
}
```

### 2. 自定义颜色标记
```json
{
  "data": {
    "elementIds": [12345],
    "visualAction": "SetColor",
    "colorValue": [0, 255, 0]
  }
}
```

### 3. 设置透明度
```json
{
  "data": {
    "elementIds": [12345, 67890],
    "visualAction": "SetTransparency",
    "transparencyValue": 70
  }
}
```

### 4. 3D聚焦
```json
{
  "data": {
    "elementIds": [12345],
    "visualAction": "SelectionBox"
  }
}
```

## 返回格式

```json
{
  "success": true,
  "message": "成功对2个元素执行SetColor操作",
  "response": {
    "processedCount": 2,
    "successfulElements": [12345, 67890],
    "failedElements": [],
    "details": {
      "appliedColor": [0, 255, 0]
    }
  }
}
```

## 典型使用场景

### 🔴 冲突可视化
```json
// 标记结构冲突的墙体
{
  "data": {
    "elementIds": [101, 102, 103],
    "visualAction": "Highlight"
  }
}
```

### 🎨 专业分类着色
```json
// 机电系统用蓝色
{
  "data": {
    "elementIds": [201, 202, 203],
    "visualAction": "SetColor",
    "colorValue": [0, 0, 255]
  }
}
```

### 👁️ 背景透明化
```json
// 设置背景构件为80%透明
{
  "data": {
    "elementIds": [301, 302, 303],
    "visualAction": "SetTransparency",
    "transparencyValue": 80
  }
}
```

### 📍 区域聚焦
```json
// 聚焦到特定房间
{
  "data": {
    "elementIds": [401],
    "visualAction": "SelectionBox"
  }
}
```

## 错误处理

### 常见错误

1. **elementIds为空**
   ```json
   {
     "success": false,
     "message": "elementIds 不能为空"
   }
   ```

2. **不支持的操作**
   ```json
   {
     "success": false,
     "message": "不支持的操作: InvalidAction，支持的操作: Select, SelectionBox, Highlight, SetColor, SetTransparency"
   }
   ```

3. **元素不存在**
   ```json
   {
     "success": false,
     "message": "视觉操作失败: 元素不存在",
     "response": {
       "processedCount": 1,
       "successfulElements": [],
       "failedElements": [
         {"elementId": 99999, "reason": "元素不存在"}
       ],
       "details": {}
     }
   }
   ```

## 技术实现

- **参数验证**: 自动规范化颜色值和透明度到合法范围
- **视图切换**: SelectionBox操作会自动切换到合适的3D视图
- **事务管理**: 所有修改操作都包装在Revit事务中
- **错误恢复**: 操作失败时自动回滚，不影响模型状态

## 性能特点

- **轻量级**: 纯视觉操作，不修改模型几何
- **快速响应**: 单个元素处理时间约10ms
- **批量支持**: 支持一次处理数百个元素
- **AI友好**: 参数简洁，Token消耗低