# ElementModify 模块

## 概述

`operate_element_modify` 命令专门处理元素参数修改与删除操作，支持批量更新属性和清理构件。

## 支持的操作

| 操作 | 说明 | 参数要求 | 事务性 |
|------|------|---------|--------|
| **SetParameter** | 修改元素参数值 | elementIds + parameterName + parameterValue | 是 |
| **Delete** | 删除元素 | elementIds | 是 |

## 参数说明

### 必填参数

- **elementIds** (number[]): 目标元素ID数组
- **modifyAction** (string): 操作类型（SetParameter / Delete）

### SetParameter 专用参数

- **parameterName** (string): 参数名称（如"高度"、"Comments"）
- **parameterValue** (string/number/boolean): 参数值
- **isBuiltInParameter** (boolean): 是否为内置参数，默认false
- **parameterType** (string): 参数类型提示（可选）：String / Double / Integer / ElementId

## 调用示例

### 1. 修改参数值

```json
{
  "data": {
    "elementIds": [12345, 67890],
    "modifyAction": "SetParameter",
    "parameterName": "Comments",
    "parameterValue": "已检查"
  }
}
```

### 2. 修改长度参数（自动单位转换）

```json
{
  "data": {
    "elementIds": [12345],
    "modifyAction": "SetParameter",
    "parameterName": "高度",
    "parameterValue": 3000,
    "parameterType": "Double"
  }
}
```

**注意**: 长度值以毫米输入，工具会自动转换为Revit内部单位（英尺）

### 3. 修改内置参数

```json
{
  "data": {
    "elementIds": [12345],
    "modifyAction": "SetParameter",
    "parameterName": "ELEM_FAMILY_PARAM",
    "parameterValue": "新类型",
    "isBuiltInParameter": true
  }
}
```

### 4. 删除元素

```json
{
  "data": {
    "elementIds": [12345, 67890, 99999],
    "modifyAction": "Delete"
  }
}
```

## 返回格式

```json
{
  "success": true,
  "message": "成功对2个元素执行SetParameter操作",
  "response": {
    "processedCount": 2,
    "successfulElements": [12345, 67890],
    "failedElements": [],
    "details": {
      "parameterName": "Comments",
      "parameterValue": "已检查"
    }
  }
}
```

## 错误处理

### 常见错误

1. **参数不存在**
   ```json
   {
     "success": false,
     "message": "修改操作部分失败：成功0个，失败1个",
     "response": {
       "failedElements": [
         {"elementId": 12345, "reason": "参数不存在: InvalidParam"}
       ]
     }
   }
   ```

2. **参数只读**
   ```json
   {
     "failedElements": [
       {"elementId": 12345, "reason": "参数为只读: ElementId"}
     ]
   }
   ```

3. **类型转换失败**
   ```json
   {
     "failedElements": [
       {"elementId": 12345, "reason": "无法将 abc 转换为 Double 类型"}
     ]
   }
   ```

## 单位转换规则

| 参数类型 | 输入单位 | Revit内部单位 | 转换公式 |
|---------|---------|--------------|---------|
| 长度 | 毫米 (mm) | 英尺 (ft) | value / 304.8 |
| 角度 | 度 (°) | 弧度 (rad) | value * π / 180 |
| 其他 | 原值 | 原值 | 无转换 |

## 最佳实践

1. **批量操作**: 一次性修改多个元素，提高效率
2. **参数验证**: 修改前先用 ai_element_filter 检查参数是否存在
3. **备份机制**: 重要修改前建议存档
4. **错误处理**: 注意检查 failedElements，分析失败原因