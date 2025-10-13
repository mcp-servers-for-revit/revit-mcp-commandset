# Revit MCP CommandSet 项目架构文档

## 项目概览

本项目是 Revit MCP 生态系统的核心组件，作为 AI 助手与 Revit 软件的通信桥梁，基于 ExternalEvent 双层架构（Command + EventHandler）为 LLM 提供访问和操作 Revit 模型的能力。

### 核心特性
- 🔗 **AI-BIM 连接**：连接大语言模型与 Revit 软件
- 🏗️ **统一架构**：基于 RevitMCPSDK 标准化开发模式
- 📊 **节点化数据**：AI 友好的结构化信息组织
- ⚡ **异步处理**：支持复杂操作的异步执行和超时控制
- 🔧 **CRUD 完整**：提供元素创建、查询、更新、删除的完整功能

### 支持版本
- Revit 2020-2024: .NET Framework 4.8
- Revit 2025+: .NET 8

## 核心依赖

- **RevitMCPSDK**：版本 `$(RevitVersion).*` - 提供统一的开发规范和基础架构
- **Revit API**：支持 Revit 2020-2025 多版本
- **Newtonsoft.Json**：JSON 序列化和数据交换

## 代码架构 & 快速导航

### 双层架构模式

```
MCP Client (AI/LLM) → [Command 层] → [EventHandler 层] → Revit API
                       参数解析      ExternalEvent触发    模型操作
```

### 核心层级结构

**Features/** - 功能模块目录（按功能组织）
- 每个模块包含：`Command.cs`（命令入口）、`EventHandler.cs`（Revit 功能实现）、`Models/`（数据模型）、`README.md`（模块文档）
- 已实现模块：ElementFilter、ElementVisual、ElementVisibility、ElementTransform、ElementModify、UnifiedCommands、RevitStatus
- **详见各模块 README.md**

**Models/** - 数据模型层
- `Common/`：通用模型（AIResult、ElementOperationResponse、CreationRequirements）
- `Geometry/`：几何模型（JZPoint、JZLine、JZPlane、JZFace、BoundingBoxInfo、LocationInfo）
- **详见 Models/README.md**（如存在）

**Utils/** - 工具类层
- `ParameterHelper.cs`：参数处理辅助（自动单位转换：mm↔ft, °↔rad）
- `GeometryUtils.cs`、`ProjectUtils.cs`：几何和项目工具
- `FamilyCreation/`、`SystemCreation/`：族和系统族创建工具
- **详见 Utils/*/README.md**

**Test/** - 测试与验证
- `Validate*.cs`：校验命令（如 ValidateCopyCommand.cs、ValidateDoorWindowFlipCommand.cs）

### 特殊层级：ElementFilter 的 FieldBuilders

ElementFilter 采用节点化架构，包含多层级 FieldBuilders：
- `FieldBuilders/Core/`：核心字段（identity、type、level）- 有 README
- `FieldBuilders/Geometry/`：几何字段（location、boundingBox、profile）- 有 README
- `FieldBuilders/Parameters/`：参数字段构建器
- **详见 Features/ElementFilter/README.md**

## 统一约束（跨模块硬规则）

### 数据格式规范 🔴 强制

**说明**：所有命令入参必须被 `"data"` 包裹
**违反后果**：缺失时会直接返回"参数格式错误：缺少 'data' 包裹层"并终止执行
**指向详情**：参考 RevitMCPSDK/API/Base 或各命令 README

### JsonProperty 同步规范 🔴 强制

**说明**：Revit 端 `[JsonProperty("属性名")]` 必须与服务端 Zod schema 属性名完全一致，使用 camelCase 命名
**违反后果**：导致参数反序列化失败，是最常见的集成问题
**指向详情**：修改前对比服务端 `src/tools/*.ts` 和本端 `Features/*/Models/*.cs` 的参数定义

### 单位换算规则 🔴 强制

**说明**：长度参数（毫米 ↔ 英尺，换算比例 304.8）、角度参数（度 ↔ 弧度，换算公式 π/180）
**违反后果**：导致元素位置、尺寸错误，影响模型准确性
**指向详情**：参考 Utils/ParameterHelper.cs 自动转换实现

### 线程安全要求 🔴 强制

**说明**：所有 Revit API 调用必须在主线程执行，通过 ExternalEvent 机制触发
**违反后果**：跨线程调用会导致 Revit 崩溃或数据损坏
**指向详情**：参考 RevitMCPSDK/API/Base/ExternalEventCommandBase

### 事务管理要求 🔴 强制

**说明**：所有修改操作必须包装在 Transaction 中（查询操作除外）
**违反后果**：未包装的修改操作会被 Revit 拒绝执行
**指向详情**：参考各 EventHandler 实现（Features/*/EventHandler.cs）

### 命名空间约定

**说明**：功能模块 `RevitMCPCommandSet.Features.{ModuleName}`、模块模型 `RevitMCPCommandSet.Features.{ModuleName}.Models`、公共模型 `RevitMCPCommandSet.Models.Common`、几何模型 `RevitMCPCommandSet.Models.Geometry`、工具类 `RevitMCPCommandSet.Utils`
**违反后果**：导致命名空间混乱，影响代码组织和查找
**自检方法**：参考现有模块的命名空间声明，确保新代码遵循相同模式

### 代码演进原则

**说明**：项目处于快速迭代阶段，代码修改时直接更新到新语义，不保留旧字段兼容
**理由**：保持代码库语义一致性，避免技术债务累积，为后续功能开发提供清晰的代码基线
**指向详情**：本文档"代码演进原则"说明

## MCP 命令清单（8个命令）

- **ai_element_filter** – 节点化元素查询 | 类别/类型/名称/空间过滤 | [→ 文档](./revit-mcp-commandset/Features/ElementFilter/README.md)
- **operate_element_visual** – 视觉操作（不改模型）| Select/Highlight/SetColor/SetTransparency | [→ 文档](./revit-mcp-commandset/Features/ElementVisual/README.md)
- **operate_element_visibility** – 可见性控制 | Hide/Isolate/Unhide/ResetIsolate | [→ 文档](./revit-mcp-commandset/Features/ElementVisibility/README.md)
- **operate_element_transform** – 几何变换 | Rotate/Mirror/Flip/Move/Copy | [→ 文档](./revit-mcp-commandset/Features/ElementTransform/README.md)
- **operate_element_modify** – 参数修改与删除 | SetParameter/Delete | [→ 文档](./revit-mcp-commandset/Features/ElementModify/README.md)
- **create_element** – 统一元素创建 | 8种族类型 + 墙体/楼板 | [→ 文档](./revit-mcp-commandset/Features/UnifiedCommands/README.md)
- **get_element_creation_suggestion** – 创建参数建议 | 分析类型并提供建议 | [→ 文档](./revit-mcp-commandset/Features/UnifiedCommands/README.md)
- **get_revit_status** – Revit 状态查询 | 版本/活动文档/当前视图 | [→ 文档](./revit-mcp-commandset/Features/RevitStatus/README.md)

## 协作流程 & README 规范

### README 使用硬规则 🔴 强制

本项目采用多层级 README 系统（根 CLAUDE.md → 功能模块 README → 子模块 README），必须遵循以下规则：

**✅ 规则1：进入前必读**
- 进入任意含 README.md 或 CLAUDE.md 的目录前，**必须**先阅读该文档
- 系统会自动 Read 该目录的 README，需理解其内容后再操作源码

**✅ 规则2：修改后同步**
- 修改代码后**必须**立即更新当前目录的 README.md，确保文档与代码同步
- 功能变更、参数调整、架构改动均需反映到文档中

**✅ 规则3：导航优先文档**
- 跨层导航优先走 **README → 源码** 路径，不接受直接 Grep/Glob 扫全仓库
- 例如：查找 ElementFilter 功能时，先读 `Features/ElementFilter/README.md`，理解结构后再定位具体文件

**违反后果：**
- 忽略 README 导致上下文失真，可能破坏现有架构约定
- 功能文档过期影响后续协作，造成技术债务

### 标准编译配置

- **配置**：Debug R20, x64
- **MSBuild 路径**：`D:\JetBrains\JetBrains Rider 2025.1.4\tools\MSBuild\Current\Bin\MSBuild.exe`
- **编译命令**：
  ```bash
  "<MSBuild路径>" "<项目路径>\RevitMCPCommandSet.csproj" -p:Configuration="Debug R20" -nologo -clp:ErrorsOnly
  ```

### 添加新功能模块（5步骤）

1. 创建功能模块目录：`Features/YourNewFeature/`
2. 创建 Command 和 EventHandler 类（继承自 RevitMCPSDK 基类）
3. 创建数据模型（如需要）：`Features/YourNewFeature/Models/*.cs`
4. 更新命名空间：`RevitMCPCommandSet.Features.YourNewFeature`
5. 更新 `command.json`：注册新命令并编写模块 README.md

### 验证与测试

- **校验脚本位置**：`Test/Validate*.cs`（如 ValidateCopyCommand.cs、ValidateDoorWindowFlipCommand.cs）
- **测试计划**：参考 `doc/MCP_TEST_PLAN.md`（如存在）

## 参考资料 & 上下文

### 关键文档

- **功能模块文档**：各 Features/*/README.md（包含功能说明、参数定义、使用示例）
- **工具类文档**：Utils/FamilyCreation/README.md、Utils/SystemCreation/README.md（如存在）
- **测试计划**：doc/MCP_TEST_PLAN.md（包含各模块测试场景）
- **构建脚本**：RevitMCPCommandSet.csproj（多版本配置）

### 更多背景

- **变更日志**：doc/CHANGELOG.md（如存在）
- **开发指南**：doc/DEVELOPMENT.md（如存在）
- **RevitMCPSDK 文档**：外部依赖的开发规范

## 实施状态

### P0 阶段（已完成 ✅）- 2025年9月
- 统一返回格式 (AIResult, ElementOperationResponse)
- operate_element_visual 完整实现
- operate_element_visibility 完整实现
- 架构文档更新

### P1 阶段（已完成 ✅）- 2025年9月底
- operate_element_modify 完整实现 (SetParameter, Delete)
- operate_element_transform 完整实现 (Rotate, Mirror, Flip, Move, Copy)
- Move 操作 - directTransform 策略（支持独立族实例、墙体、楼板）
- Copy 操作完整实现
- 几何模型扩展 (JZPlane, JZVector)
- ParameterHelper 工具类（自动单位转换）

**当前默认语义：** 项目默认使用 P1 完整能力，不考虑旧版本兼容（遵循代码演进原则）

### P2 阶段（规划中）
- Move 操作 - recreate 策略（支持有宿主族实例、基于线/面的族实例）
- 高级参数处理（公式参数、批量模板、映射规则）
- 性能优化（批量处理、事务合并、内存优化）
- 扩展功能（validateOnly 模式、transactionPolicy 配置、回滚机制）
- 新增操作类型（Scale 缩放、Align 对齐等）

---

更多详细信息请参考项目源码和各模块 README.md 文档。
