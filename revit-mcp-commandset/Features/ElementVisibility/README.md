# ElementVisibility 模块

## 概述

`operate_element_visibility` 命令专门处理当前视图的可见性管理，帮助用户控制元素的显示和隐藏，实现视图清理和专业系统聚焦。

## 支持的操作

| 操作 | 说明 | 参数要求 | 持久性 |
|------|------|---------|--------|
| **Hide** | 持久隐藏元素 | elementIds | 直到手动显示 |
| **TempHide** | 临时隐藏元素 | elementIds | 临时隐藏 |
| **Isolate** | 隔离显示（仅显示选定元素） | elementIds | 临时模式 |
| **Unhide** | 取消隐藏元素 | elementIds | 恢复显示 |
| **ResetIsolate** | 重置隔离模式 | 无需elementIds | 恢复正常视图 |

## 参数说明

### 必填参数

- **visibilityAction** (string): 操作类型，必须是枚举值之一

### 条件必填参数

- **elementIds** (number[]): 目标元素ID数组
  - ✅ **Hide/TempHide/Isolate/Unhide**: 必须提供
  - ⬜ **ResetIsolate**: 可以不提供

## 调用示例

### 1. 隐藏无关构件
```json
{
  "data": {
    "elementIds": [12345, 67890, 11111],
    "visibilityAction": "Hide"
  }
}
```

### 2. 隔离显示特定系统
```json
{
  "data": {
    "elementIds": [201, 202, 203],
    "visibilityAction": "Isolate"
  }
}
```

### 3. 临时隐藏
```json
{
  "data": {
    "elementIds": [301, 302],
    "visibilityAction": "TempHide"
  }
}
```

### 4. 取消隐藏
```json
{
  "data": {
    "elementIds": [12345, 67890],
    "visibilityAction": "Unhide"
  }
}
```

### 5. 重置隔离模式（无需elementIds）
```json
{
  "data": {
    "visibilityAction": "ResetIsolate"
  }
}
```

## 返回格式

### 成功返回示例
```json
{
  "success": true,
  "message": "成功对3个元素执行Isolate操作",
  "response": {
    "processedCount": 3,
    "successfulElements": [201, 202, 203],
    "failedElements": [],
    "details": {}
  }
}
```

### ResetIsolate 返回示例
```json
{
  "success": true,
  "message": "成功重置隔离模式",
  "response": {
    "processedCount": 0,
    "successfulElements": [],
    "failedElements": [],
    "details": {}
  }
}
```

## 典型使用场景

### 🏗️ 专业系统管理
```json
// 1. 隔离机电系统
{
  "data": {
    "elementIds": [1001, 1002, 1003],
    "visibilityAction": "Isolate"
  }
}

// 2. 检查完成后恢复正常视图
{
  "data": {
    "visibilityAction": "ResetIsolate"
  }
}
```

### 🔧 施工阶段管理
```json
// 隐藏未施工的构件
{
  "data": {
    "elementIds": [2001, 2002, 2003],
    "visibilityAction": "TempHide"
  }
}
```

### 🎯 问题检查工作流
```json
// 1. 隔离问题构件
{
  "data": {
    "elementIds": [3001, 3002],
    "visibilityAction": "Isolate"
  }
}

// 2. 检查完成后恢复
{
  "data": {
    "visibilityAction": "ResetIsolate"
  }
}
```

### 👀 视图清理
```json
// 隐藏注释和标注
{
  "data": {
    "elementIds": [4001, 4002, 4003, 4004],
    "visibilityAction": "Hide"
  }
}
```

## 操作类型详解

### Hide vs TempHide

| 特性 | Hide | TempHide |
|------|------|----------|
| **持久性** | 持久隐藏 | 临时隐藏 |
| **恢复方式** | 需要主动Unhide | 视图切换自动恢复 |
| **用途** | 长期不需要的元素 | 短期遮挡 |

### Isolate vs Hide

| 特性 | Isolate | Hide |
|------|---------|------|
| **显示逻辑** | 仅显示选定元素 | 隐藏选定元素 |
| **其他元素** | 自动隐藏 | 保持原状 |
| **用途** | 专注特定构件 | 移除干扰 |

## 错误处理

### 常见错误

1. **操作需要elementIds但未提供**
   ```json
   {
     "success": false,
     "message": "Hide 操作需要提供 elementIds"
   }
   ```

2. **不支持的操作类型**
   ```json
   {
     "success": false,
     "message": "不支持的操作: InvalidAction，支持的操作: Hide, TempHide, Isolate, Unhide, ResetIsolate"
   }
   ```

3. **元素不存在**
   ```json
   {
     "success": false,
     "message": "可见性操作失败: 指定的元素不存在",
     "response": {
       "processedCount": 1,
       "successfulElements": [],
       "failedElements": [
         {"elementId": 99999, "reason": "指定的元素不存在"}
       ],
       "details": {}
     }
   }
   ```

## 技术实现

### 事务管理
所有可见性操作都包装在Revit事务中，确保操作的原子性：
```csharp
using (Transaction trans = new Transaction(doc, "隐藏元素"))
{
    trans.Start();
    doc.ActiveView.HideElements(elementIds);
    trans.Commit();
}
```

### 视图依赖
所有操作都基于当前活动视图，只影响当前视图的显示状态。

### 状态管理
- **Hide**: 在视图中永久记录隐藏状态
- **TempHide**: 临时状态，不保存到文件
- **Isolate**: 激活临时隔离模式
- **ResetIsolate**: 清除所有临时隐藏/隔离状态

## 性能特点

- **快速响应**: 单个元素处理时间约5ms
- **批量支持**: 支持一次处理数百个元素
- **视图特定**: 仅影响当前视图，不影响其他视图
- **内存友好**: 临时操作不增加文件大小

## 最佳实践

1. **工作流程化**: 使用 Isolate → 检查 → ResetIsolate 的标准流程
2. **临时优先**: 优先使用 TempHide 而非 Hide，减少意外的永久隐藏
3. **批量操作**: 一次性隐藏多个相关元素，提高效率
4. **状态清理**: 定期使用 ResetIsolate 清理视图状态