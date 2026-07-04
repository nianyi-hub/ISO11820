# ISO 11820 建筑材料不燃性试验仿真系统

>  **第十八届全国大学生软件创新大赛** 参赛作品

##  项目简介

ISO 11820 建筑材料不燃性试验仿真系统是一款 **Windows 桌面应用**，用于模拟建筑材料不燃性试验的完整流程。系统通过软件仿真生成温度数据，用户按照真实试验操作流程完成试验，并自动生成符合标准格式的测试报告。

### 核心流程

```
登录 → 新建试验 → 开始升温(仿真至750°C) → 温度稳定 → 开始记录(60分钟) → 试验结束 → 导出报告
```

##  主要功能

| 模块 | 功能描述 |
|------|---------|
| **用户登录** | 管理员/试验员双角色登录，密码鉴权 |
| **试验管理** | 新建试验、填写样品信息、设置试验参数 |
| **温度仿真** | 5 通道温度实时仿真（炉温×2、表面温、中心温、校准温） |
| **状态机控制** | Idle → Preparing → Ready → Recording → Complete 自动流转 |
| **实时显示** | LED 风格温度面板 + OxyPlot 曲线图 + 系统消息日志 |
| **温漂计算** | 基于 MathNet 线性回归，实时计算 10 分钟温度漂移 |
| **数据导出** | CSV / Excel（含图表）/ PDF 三种格式报告 |
| **历史查询** | 按日期范围、样品编号、操作员查询历史试验记录 |
| **设备校准** | 传感器标定记录与管理 |

##  技术架构

```
┌─────────────────────────────────┐
│         UI 层 (WinForms)         │
│   LoginForm / MainForm / ...    │
├─────────────────────────────────┤
│         Core 核心层               │
│   TestMasterController (状态机)   │
├─────────────────────────────────┤
│        Services 服务层            │
│   DaqWorker / SensorSimulator   │
│   ExportService                 │
├─────────────────────────────────┤
│         Data 数据层               │
│   DbHelper / SQLite             │
└─────────────────────────────────┘
```

##  技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 8.0 | 开发框架 |
| WinForms | — | 桌面 UI 框架 |
| SQLite | — | 本地数据库 |
| OxyPlot | 2.2.0 | 实时温度曲线图 |
| EPPlus | 7.4.2 | Excel 报告生成 |
| PDFsharp-MigraDoc | 6.2.0 | PDF 报告生成 |
| QuestPDF | 2026.6.1 | PDF 报告生成（辅助） |
| MathNet.Numerics | 5.0.0 | 线性回归（温漂计算） |
| Serilog | 4.1.0 | 结构化日志 |
| Microsoft.Data.Sqlite | 8.0.11 | 数据库驱动 |

##  快速开始

### 环境要求

- **操作系统**：Windows 10 / 11
- **.NET SDK**：.NET 8.0
- **IDE**：Visual Studio 2022（推荐）或 JetBrains Rider

### 克隆与运行

```bash
# 克隆仓库
git clone <your-repo-url>
cd ISO11820

# 还原依赖
dotnet restore

# 运行项目
dotnet run --project src/ISO11820System.csproj
```

### 默认账号

| 角色 | 用户名 | 密码 |
|------|--------|------|
| 管理员 | admin | 123456 |
| 试验员 | experimenter | 123456 |

##  项目结构

```
ISO11820/
├── ISO11820.sln                          # 解决方案文件
├── src/                                  # 主项目
│   ├── ISO11820System.csproj             # 项目文件
│   ├── Program.cs                        # 入口点
│   ├── Core/
│   │   └── TestMasterController.cs       # 试验控制器（状态机核心）
│   ├── Models/
│   │   ├── TestMaster.cs                 # 试验记录模型
│   │   ├── ProductMaster.cs              # 样品模型
│   │   ├── Operator.cs                   # 操作员模型
│   │   ├── Apparatus.cs                  # 设备模型
│   │   ├── Sensor.cs                     # 传感器模型
│   │   ├── TestState.cs                  # 状态枚举
│   │   └── CalibrationRecord.cs          # 校准记录模型
│   ├── Services/
│   │   ├── Simulation/
│   │   │   ├── DaqWorker.cs              # 数据采集工作器
│   │   │   ├── SensorSimulator.cs        # 传感器仿真引擎
│   │   │   └── DataBroadcastEventArgs.cs # 数据广播事件参数
│   │   └── Export/
│   │       └── ExportService.cs          # 导出服务（CSV/Excel/PDF）
│   ├── Data/
│   │   └── DbHelper.cs                   # 数据库帮助类
│   ├── Form/
│   │   ├── LoginForm.cs                  # 登录窗体
│   │   ├── MainForm.cs                   # 主窗体
│   │   ├── NewTestForm.cs                # 新建试验窗体
│   │   └── TestRecordForm.cs             # 试验记录窗体
│   └── Utilities/
│       ├── AppContext.cs                 # 全局应用上下文
│       ├── ConfigManager.cs              # 配置管理器
│       └── Logger.cs                     # 日志工具
├── ISO11820System.Tests/                 # 单元测试项目
└── DB-数据库设计.md                       # 数据库设计文档
```

##  数据库设计

系统使用 SQLite 本地数据库，共 6 张表：

| 表名 | 说明 |
|------|------|
| `operators` | 操作员/用户账号 |
| `apparatus` | 试验设备信息 |
| `productmaster` | 样品信息 |
| `testmaster` | 试验记录（核心表） |
| `sensors` | 传感器通道配置 |
| `CalibrationRecords` | 设备校准历史 |



##  仿真引擎

系统仿真 **5 个温度通道**，无需真实硬件：

| 通道 | 代号 | 行为描述 |
|------|------|---------|
| 炉温1 | TF1 | 升温至 750°C 并稳定 |
| 炉温2 | TF2 | 与 TF1 同步（独立噪声） |
| 表面温 | TS | 记录阶段向炉温 × 0.95 指数接近 |
| 中心温 | TC | 记录阶段向炉温 × 0.85 指数接近 |
| 校准温 | TCal | = TF1 + 随机波动 × 2 |

### 状态机

```
Idle ──开始升温──▶ Preparing ──温度稳定──▶ Ready ──开始记录──▶ Recording ──到达时长/终止条件──▶ Complete
  ▲                    ▲                      │                                              │
  └──停止加热──────────┘                      └──温度跌出范围──────────────────────────────────┘
```

##  配置说明

关键配置项在 `src/appsettings.json` 中：

```json
{
  "Simulation": {
    "EnableSimulation": true,        // 仿真模式开关
    "InitialFurnaceTemp": 720.0,     // 初始炉温 (°C)
    "TargetFurnaceTemp": 750.0,      // 目标炉温 (°C)
    "HeatingRatePerSecond": 40.0,    // 升温速率 (°C/s)
    "TempFluctuation": 0.5           // 温度波动幅度 (°C)
  },
  "FileStorage": {
    "BaseDirectory": "D:\\ISO11820"  // 文件存储根目录
  }
}
```

