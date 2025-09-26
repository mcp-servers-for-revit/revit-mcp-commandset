# ElementTransform 模块

## 概述

`operate_element_transform` 命令专门处理元素几何变换操作，支持旋转、镜像、翻转和移动。

## 支持的操作

| 操作 | 说明 | 参数要求 | P1支持度 |
|------|------|---------|---------|
| **Rotate** | 绕指定轴旋转元素 | elementIds + rotateAxis + rotateAngle | ✅ 完全支持 |
| **Mirror** | 沿平面镜像元素 | elementIds + mirrorPlane | ✅ 完全支持 |
| **Flip** | 翻转族实例 | elementIds + flipDirection | ✅ 完全支持 |
| **Move** | 移动元素 | elementIds + moveVector | ⚠️ 仅directTransform |

## 参数说明

### 必填参数

- **elementIds** (number[]): 目标元素ID数组
- **transformAction** (string): 操作类型（Rotate / Mirror / Flip / Move）

### Rotate 专用参数

- **rotateAxis** (JZLine): 旋转轴线
  - p0: 起点坐标（毫米）
  - p1: 终点坐标（毫米）
- **rotateAngle** (number): 旋转角度（度数，正值为逆时针）

### Mirror 专用参数

- **mirrorPlane** (JZPlane): 镜像平面
  - origin: 平面原点（毫米）
  - normal: 平面法向量（归一化）

### Flip 专用参数

- **flipDirection** (string): 翻转方向
  - "Hand": 左右翻转
  - "Facing": 前后翻转

### Move 专用参数

- **moveVector** (JZVector): 移动向量（毫米）
  - x, y, z: 三个方向的位移量
- **moveStrategy** (string): 移动策略
  - "directTransform": 直接变换（P1支持）
  - "recreate": 重新创建（P2实现）

## 调用示例

### 1. 旋转元素

```json
{
  "data": {
    "elementIds": [12345],
    "transformAction": "Rotate",
    "rotateAxis": {
      "p0": {"x": 0, "y": 0, "z": 0},
      "p1": {"x": 0, "y": 0, "z": 1000}
    },
    "rotateAngle": 90
  }
}
```

### 2. 镜像元素

```json
{
  "data": {
    "elementIds": [12345, 67890],
    "transformAction": "Mirror",
    "mirrorPlane": {
      "origin": {"x": 0, "y": 0, "z": 0},
      "normal": {"x": 1, "y": 0, "z": 0}
    }
  }
}
```

### 3. 翻转族实例

```json
{
  "data": {
    "elementIds": [12345],
    "transformAction": "Flip",
    "flipDirection": "Hand"
  }
}
```

### 4. 移动元素（P1：directTransform）

```json
{
  "data": {
    "elementIds": [12345],
    "transformAction": "Move",
    "moveVector": {"x": 1000, "y": 2000, "z": 0},
    "moveStrategy": "directTransform"
  }
}
```

## Move操作策略说明 ⚠️

### directTransform 策略（P1已实现）

**适用范围**:
- ✅ 独立族实例（无宿主）
- ✅ 墙体
- ✅ 楼板
- ❌ 有宿主的族实例（门、窗等）
- ❌ 基于线的族实例
- ❌ 基于面的族实例

**失败兜底**:
- 遇到不支持的元素类型时，返回失败原因
- 错误信息提示："元素类型不支持directTransform移动（建议使用recreate策略，P2实现）"

### recreate 策略（P2规划中）

**说明**: 适用于复杂场景，通过删除后重建实现移动，将在P2阶段实现。

## 返回格式

```json
{
  "success": true,
  "message": "成功对1个元素执行Rotate操作",
  "response": {
    "processedCount": 1,
    "successfulElements": [12345],
    "failedElements": [],
    "details": {
      "rotateAngle": 90,
      "rotateAxis": {
        "p0": {"x": 0, "y": 0, "z": 0},
        "p1": {"x": 0, "y": 0, "z": 1000}
      }
    }
  }
}
```

## 错误处理

### Move操作特殊错误

```json
{
  "success": false,
  "message": "Move操作部分失败：成功0个，失败1个",
  "response": {
    "failedElements": [
      {
        "elementId": 12345,
        "reason": "元素类型不支持directTransform移动（建议使用recreate策略，P2实现）"
      }
    ],
    "details": {
      "strategyNote": "P1阶段仅支持directTransform，复杂场景（如宿主元素）将在P2实现recreate策略"
    }
  }
}
```

## 最佳实践

1. **旋转轴选择**: 优先使用垂直轴（Z轴）旋转，兼容性最好
2. **镜像平面**: 使用标准平面（XY/YZ/XZ）可提高成功率
3. **翻转检查**: 翻转前用 ai_element_filter 检查元素是否支持翻转
4. **移动策略**: P1阶段优先移动独立族实例，有宿主元素等P2 recreate策略