using ISO11820System.Models;
using ISO11820System.Services;
using ISO11820System.Data;
using ISO11820System.Utilities;
using MathNet.Numerics;

namespace ISO11820System.Core
{
    /// <summary>
    /// 试验控制器（状态机 + 试验流程控制）
    /// </summary>
    public class TestMasterController
    {
        private readonly SensorSimulator _simulator;
        private readonly DaqWorker _daqWorker;
        private readonly DbHelper _dbHelper;

        // 当前状态
        public TestState CurrentState { get; private set; } = TestState.Idle;

        // 当前试验信息
        public TestMaster? CurrentTest { get; private set; }

        // 温度数据缓存（记录阶段）
        private List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> _tempDataCache = new();

        // 图表数据缓存（所有阶段，用于OxyPlot实时曲线）
        private List<ChartDataPoint> _chartDataCache = new();
        private double _chartElapsedTime = 0;

        // 计时器
        private int _recordedSeconds = 0;
        private System.Threading.Timer? _recordTimer;

        // 温漂计算（最近10分钟数据）
        private Queue<double> _tf1History = new();
        private Queue<double> _tf2History = new();
        private const int DRIFT_WINDOW_SIZE = 600; // 10分钟 = 600秒

        // 系统消息队列
        private List<MasterMessage> _pendingMessages = new();

        // 试验配置
        /// <summary>试验时长模式：true=标准60分钟, false=自定义时长</summary>
        public bool IsStandardDuration { get; set; } = true;
        /// <summary>自定义试验时长（秒）</summary>
        public int CustomDurationSeconds { get; set; } = 3600;
        /// <summary>目标试验时长（秒）</summary>
        public int TargetDurationSeconds => IsStandardDuration ? 3600 : CustomDurationSeconds;

        // 终止条件检查标记
        private HashSet<int> _checkedMilestones = new();

        // 事件：数据广播
        public event EventHandler<DataBroadcastEventArgs>? DataBroadcast;

        public TestMasterController(DbHelper dbHelper, SensorSimulator simulator, DaqWorker daqWorker)
        {
            _dbHelper = dbHelper;
            _simulator = simulator;
            _daqWorker = daqWorker;

            // 订阅数据更新事件
            _daqWorker.DataUpdated += OnDaqDataUpdated;
        }

        /// <summary>
        /// 启动控制器
        /// </summary>
        public void Start()
        {
            _daqWorker.Start();
            var operatorName = AppGlobals.Instance.CurrentUser?.Username ?? "未知";
            AddMessage($"系统初始化，操作员：{operatorName}", MessageType.Info);
        }

        /// <summary>
        /// 停止控制器
        /// </summary>
        public void Stop()
        {
            _daqWorker.Stop();
            _recordTimer?.Dispose();
        }

        /// <summary>
        /// 创建新试验
        /// </summary>
        public void CreateNewTest(TestMaster test, bool isStandardDuration = true, int customDurationSeconds = 3600)
        {
            CurrentTest = test;
            IsStandardDuration = isStandardDuration;
            CustomDurationSeconds = customDurationSeconds;
            _tempDataCache.Clear();
            _chartDataCache.Clear();
            _chartElapsedTime = 0;
            _recordedSeconds = 0;
            _checkedMilestones.Clear();

            // 保存到数据库
            _dbHelper.CreateTestRecord(test);

            AddMessage($"创建新试验: {test.ProductId}", MessageType.Info);
        }

        /// <summary>
        /// 开始升温
        /// </summary>
        public void StartHeating()
        {
            if (CurrentState != TestState.Idle) return;
            if (CurrentTest == null) return;

            CurrentState = TestState.Preparing;
            _simulator.StartHeating();
            _tf1History.Clear();
            _tf2History.Clear();
            _chartDataCache.Clear();
            _chartElapsedTime = 0;

            AddMessage("开始升温，系统升温中", MessageType.Info);
        }

        /// <summary>
        /// 停止升温
        /// </summary>
        public void StopHeating()
        {
            if (CurrentState != TestState.Preparing && CurrentState != TestState.Ready) return;

            CurrentState = TestState.Idle;
            _simulator.StopHeating();

            AddMessage("停止加热", MessageType.Info);
        }

        /// <summary>
        /// 开始记录
        /// </summary>
        public void StartRecording()
        {
            if (CurrentState != TestState.Ready) return;

            // 检查是否有未保存的试验
            if (HasUnsavedCompleteTest())
                return;

            // 计算恒功率值（PID队列平均值）
            int constPower = (int)Math.Round(_simulator.GetAveragePidOutput());
            AddMessage($"恒功率值: {constPower}", MessageType.Info);

            CurrentState = TestState.Recording;
            _simulator.StartRecording();
            _recordedSeconds = 0;
            _tempDataCache.Clear();
            _checkedMilestones.Clear();

            // 启动每秒记录定时器
            _recordTimer = new System.Threading.Timer(OnRecordTick, null, 0, 1000);

            AddMessage("开始记录，计时开始", MessageType.Info);
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        public void StopRecording()
        {
            if (CurrentState != TestState.Recording) return;

            _recordTimer?.Dispose();
            _recordTimer = null;

            CurrentState = TestState.Complete;
            _simulator.StopRecording();

            AddMessage($"用户手动停止记录，共记录 {_recordedSeconds} 秒", MessageType.Info);

            // 判断是否有有效记录
            if (_tempDataCache.Count == 0)
            {
                AddMessage("无有效记录数据，回到升温状态", MessageType.Warning);
                CurrentState = TestState.Preparing;
            }
        }

        /// <summary>
        /// 保存试验结果
        /// </summary>
        public void SaveTestResult(double postWeight, bool hasFlame, int flameTime, int flameDuration, string memo = "")
        {
            if (CurrentTest == null || _tempDataCache.Count == 0)
            {
                AddMessage("没有可保存的试验数据", MessageType.Warning);
                return;
            }

            // 计算失重
            CurrentTest.PostWeight = postWeight;
            CurrentTest.LostWeight = CurrentTest.PreWeight - postWeight;
            CurrentTest.LostWeightPercent = (CurrentTest.LostWeight / CurrentTest.PreWeight) * 100;

            // 记录火焰信息
            CurrentTest.FlameTime = hasFlame ? flameTime : 0;
            CurrentTest.FlameDuration = hasFlame ? flameDuration : 0;

            // 记录备注
            CurrentTest.Memo = memo;

            // 记录时长
            CurrentTest.TotalTestTime = _recordedSeconds;

            // 恒功率值
            CurrentTest.ConstPower = (int)Math.Round(_simulator.GetAveragePidOutput());

            // 计算温度统计
            CalculateTempStats();

            // 更新数据库
            CurrentTest.Flag = "10000000"; // 标记为已完成
            _dbHelper.UpdateTestResult(CurrentTest);

            // 导出CSV
            ExportCsvData();

            AddMessage("试验记录已保存", MessageType.Success);

            // 清空当前试验
            CurrentTest = null;
            _tempDataCache.Clear();
        }

        /// <summary>
        /// 检查是否有已完成但未保存的试验
        /// </summary>
        public bool HasUnsavedCompleteTest()
        {
            return _recordedSeconds > 0 &&
                   CurrentTest != null &&
                   CurrentTest.Flag != "10000000";
        }

        /// <summary>
        /// 数据采集更新回调
        /// </summary>
        private void OnDaqDataUpdated(object? sender, (double TF1, double TF2, double TS, double TC, double TCal) data)
        {
            // 更新温漂历史
            _tf1History.Enqueue(data.TF1);
            _tf2History.Enqueue(data.TF2);
            if (_tf1History.Count > DRIFT_WINDOW_SIZE)
            {
                _tf1History.Dequeue();
                _tf2History.Dequeue();
            }

            // 更新图表数据缓存
            _chartElapsedTime += 0.8;
            _chartDataCache.Add(new ChartDataPoint
            {
                Time = _chartElapsedTime,
                TF1 = data.TF1,
                TF2 = data.TF2,
                TS = data.TS,
                TC = data.TC
            });

            // 状态机逻辑
            UpdateStateMachine(data);

            // 广播数据到UI
            BroadcastData(data);
        }

        /// <summary>
        /// 记录定时器回调（每秒记录一次）
        /// </summary>
        private void OnRecordTick(object? state)
        {
            if (CurrentState != TestState.Recording) return;

            var (TF1, TF2, TS, TC, TCal) = _daqWorker.GetCurrentTemp();
            _tempDataCache.Add((_recordedSeconds, TF1, TF2, TS, TC, TCal));
            _recordedSeconds++;

            // 检查终止条件
            CheckTerminationConditions();

            // 检查是否到达目标时长
            if (_recordedSeconds >= TargetDurationSeconds)
            {
                _recordTimer?.Dispose();
                _recordTimer = null;
                CurrentState = TestState.Complete;
                _simulator.StopRecording();
                AddMessage($"记录时间到达 {TargetDurationSeconds} 秒，试验自动结束", MessageType.Info);
            }
        }

        /// <summary>
        /// 检查试验终止条件（标准模式每5分钟检查）
        /// </summary>
        private void CheckTerminationConditions()
        {
            // 仅在标准60分钟模式下检查
            if (!IsStandardDuration) return;

            // 检查时间点：30, 35, 40, 45, 50, 55分钟
            int[] checkPoints = { 1800, 2100, 2400, 2700, 3000, 3300 };

            foreach (var checkpoint in checkPoints)
            {
                if (_recordedSeconds >= checkpoint && !_checkedMilestones.Contains(checkpoint))
                {
                    _checkedMilestones.Add(checkpoint);

                    // 检查10分钟温漂是否都满足要求
                    double driftTF1 = CalculateTempDrift(_tf1History);
                    double driftTF2 = CalculateTempDrift(_tf2History);

                    // 复用炉温稳定条件：10分钟温漂有效且不超过阈值
                    double maxDrift = 2.0; // °C/10min 阈值
                    if (Math.Abs(driftTF1) <= maxDrift && Math.Abs(driftTF2) <= maxDrift &&
                        _tf1History.Count >= 600)
                    {
                        _recordTimer?.Dispose();
                        _recordTimer = null;
                        CurrentState = TestState.Complete;
                        _simulator.StopRecording();
                        AddMessage($"满足终止条件，试验结束（{_recordedSeconds / 60}分钟）", MessageType.Warning);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// 状态机更新逻辑
        /// </summary>
        private void UpdateStateMachine((double TF1, double TF2, double TS, double TC, double TCal) data)
        {
            switch (CurrentState)
            {
                case TestState.Preparing:
                    // 检查是否达到就绪条件
                    if (_simulator.CheckReadyCriteria())
                    {
                        CurrentState = TestState.Ready;
                        AddMessage("温度已稳定，可以开始记录", MessageType.Info);
                    }
                    break;

                case TestState.Ready:
                    // 检查温度是否跌出稳定范围
                    if (data.TF1 < 745 || data.TF1 > 755)
                    {
                        CurrentState = TestState.Preparing;
                        AddMessage("温度超出稳定范围，回到升温状态", MessageType.Warning);
                    }
                    break;

                case TestState.Complete:
                    // 完成后自动回到Preparing保持恒温（仅当已保存完成或无未保存数据时）
                    if (CurrentTest == null || !string.IsNullOrEmpty(CurrentTest.Flag))
                    {
                        CurrentState = TestState.Preparing;
                    }
                    break;
            }
        }

        /// <summary>
        /// 广播数据到UI
        /// </summary>
        private void BroadcastData((double TF1, double TF2, double TS, double TC, double TCal) data)
        {
            var args = new DataBroadcastEventArgs
            {
                TF1 = data.TF1,
                TF2 = data.TF2,
                TS = data.TS,
                TC = data.TC,
                TCal = data.TCal,
                State = CurrentState,
                RecordedSeconds = _recordedSeconds,
                TempDrift = CalculateTempDrift(_tf1History),
                Messages = new List<MasterMessage>(_pendingMessages),
                CurrentProductId = CurrentTest?.ProductId ?? "",
                HasUnsavedCompleteTest = HasUnsavedCompleteTest(),
                ChartData = new List<ChartDataPoint>(_chartDataCache)
            };

            _pendingMessages.Clear();

            DataBroadcast?.Invoke(this, args);
        }

        /// <summary>
        /// 计算温度漂移（°C/10min）使用MathNet线性回归
        /// </summary>
        private double CalculateTempDrift(Queue<double> history)
        {
            if (history.Count < 10) return 0;

            var data = history.ToArray();
            var x = Enumerable.Range(0, data.Length).Select(i => (double)i).ToArray();
            var y = data;

            var (intercept, slope) = Fit.Line(x, y);
            return slope * 600; // 转换为每10分钟的漂移
        }

        /// <summary>
        /// 计算温度统计数据
        /// </summary>
        private void CalculateTempStats()
        {
            if (CurrentTest == null || _tempDataCache.Count == 0) return;

            var first = _tempDataCache.First();
            var last = _tempDataCache.Last();

            // 最大值
            CurrentTest.MaxTF1 = _tempDataCache.Max(d => d.TF1);
            CurrentTest.MaxTF2 = _tempDataCache.Max(d => d.TF2);
            CurrentTest.MaxTS = _tempDataCache.Max(d => d.TS);
            CurrentTest.MaxTC = _tempDataCache.Max(d => d.TC);

            var maxTF1Data = _tempDataCache.First(d => d.TF1 == CurrentTest.MaxTF1);
            CurrentTest.MaxTF1Time = maxTF1Data.Time;
            var maxTF2Data = _tempDataCache.First(d => d.TF2 == CurrentTest.MaxTF2);
            CurrentTest.MaxTF2Time = maxTF2Data.Time;
            var maxTSData = _tempDataCache.First(d => d.TS == CurrentTest.MaxTS);
            CurrentTest.MaxTSTime = maxTSData.Time;
            var maxTCData = _tempDataCache.First(d => d.TC == CurrentTest.MaxTC);
            CurrentTest.MaxTCTime = maxTCData.Time;

            // 最终值
            CurrentTest.FinalTF1 = last.TF1;
            CurrentTest.FinalTF2 = last.TF2;
            CurrentTest.FinalTS = last.TS;
            CurrentTest.FinalTC = last.TC;
            CurrentTest.FinalTF1Time = last.Time;
            CurrentTest.FinalTF2Time = last.Time;
            CurrentTest.FinalTSTime = last.Time;
            CurrentTest.FinalTCTime = last.Time;

            // 温升
            CurrentTest.DeltaTF1 = CurrentTest.FinalTF1 - CurrentTest.AmbientTemp;
            CurrentTest.DeltaTF2 = CurrentTest.FinalTF2 - CurrentTest.AmbientTemp;
            CurrentTest.DeltaTS = CurrentTest.FinalTS - CurrentTest.AmbientTemp;
            CurrentTest.DeltaTC = CurrentTest.FinalTC - CurrentTest.AmbientTemp;
            CurrentTest.DeltaTF = CurrentTest.DeltaTS; // 样品温升取表面温升
        }

        /// <summary>
        /// 导出CSV数据
        /// </summary>
        private void ExportCsvData()
        {
            if (CurrentTest == null) return;

            var baseDir = AppGlobals.Instance.Config.TestDataDirectory;
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");

            var csvPath = Path.Combine(baseDir, CurrentTest.ProductId, CurrentTest.TestId, "sensor_data.csv");

            var exporter = new ExportService();
            exporter.ExportCsv(_tempDataCache, csvPath);
        }

        /// <summary>
        /// 添加系统消息
        /// </summary>
        private void AddMessage(string message, MessageType type = MessageType.Info)
        {
            _pendingMessages.Add(new MasterMessage
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Message = message,
                Type = type
            });
        }

        /// <summary>
        /// 获取温度数据缓存
        /// </summary>
        public List<(int Time, double TF1, double TF2, double TS, double TC, double TCal)> GetTempDataCache()
        {
            return new List<(int, double, double, double, double, double)>(_tempDataCache);
        }

        /// <summary>
        /// 获取当前5通道温度值
        /// </summary>
        public (double TF1, double TF2, double TS, double TC, double TCal) GetCurrentTemperatures()
        {
            return _daqWorker.GetCurrentTemp();
        }
    }
}
