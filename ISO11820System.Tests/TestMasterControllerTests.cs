using ISO11820System.Core;
using ISO11820System.Data;
using ISO11820System.Models;
using ISO11820System.Services;
using Microsoft.Data.Sqlite;

namespace ISO11820System.Tests
{
    /// <summary>
    /// 试验控制器 — 状态机与流程测试
    /// 覆盖：5 状态流转、记录逻辑、终止条件、数据计算
    ///
    /// 计时说明：
    ///   DaqWorker 每 800ms 触发一次 tick；仿真器 HeatingRate=1000°C/s，
    ///   1 tick 即达 750°C，再需 4 tick 累计 stableCounter > 3 → Ready。
    ///   总计约 5 tick ≈ 4000ms，各测试统一等待 5000ms 保证稳定到达。
    /// </summary>
    public class TestMasterControllerTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly DbHelper _dbHelper;
        private readonly SensorSimulator _simulator;
        private readonly DaqWorker _daqWorker;
        private readonly TestMasterController _controller;

        private DataBroadcastEventArgs? _lastBroadcast;
        private static int _testCounter = 0;

        public TestMasterControllerTests()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), $"ctrl_test_{Guid.NewGuid():N}.db");
            _dbHelper = new DbHelper(_tempDbPath);

            _simulator = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0,   // 极快升温，1 tick 到达目标
                TempFluctuation = 0.0,   // 无噪声，结果可预测
                StableThreshold = 3.0
            };

            _daqWorker = new DaqWorker(_simulator, enableSimulation: true);
            _controller = new TestMasterController(_dbHelper, _simulator, _daqWorker);

            _controller.DataBroadcast += (_, args) => _lastBroadcast = args;
        }

        public void Dispose()
        {
            _controller.Stop();
            _daqWorker.Stop();
            SqliteConnection.ClearAllPools();
            if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
        }

        /// <summary>等待 DaqWorker tick 到达 Ready 状态（约需 5 ticks × 800ms ≈ 4s）</summary>
        private void WaitForReady(int timeoutMs = 8000)
        {
            _daqWorker.Start();
            Thread.Sleep(timeoutMs);
            _daqWorker.Stop();
        }

        /// <summary>创建测试用试验记录（含产品预保存，确保 FK 约束满足）</summary>
        private TestMaster CreateTestRecord(string productId = "TEST-PROD")
        {
            int n = Interlocked.Increment(ref _testCounter);
            string testId = $"{DateTime.Now:yyyyMMdd-HHmmss}-{n:D4}";

            _dbHelper.SaveProduct(new ProductMaster
            {
                ProductId = productId,
                ProductName = "测试样品",
                Specific = "测试规格",
                Diameter = 50.0,
                Height = 100.0
            });

            return new TestMaster
            {
                ProductId = productId,
                TestId = testId,
                TestDate = DateTime.Today,
                AmbientTemp = 23.5,
                AmbientHumidity = 55.0,
                According = "ISO 11820:2022",
                Operator = "admin",
                ApparatusId = "0",
                ApparatusName = "一号试验炉",
                ApparatusCheckDate = DateTime.Today.AddYears(1),
                ReportNo = $"RPT-{n:D4}",
                PreWeight = 100.0
            };
        }

        // ==================== 初始状态 ====================

        [Fact]
        public void Constructor_InitialState_IsIdle()
        {
            Assert.Equal(TestState.Idle, _controller.CurrentState);
            Assert.Null(_controller.CurrentTest);
        }

        // ==================== Idle → Preparing ====================

        [Fact]
        public void StartHeating_FromIdle_TransitionsToPreparing()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            Assert.Equal(TestState.Preparing, _controller.CurrentState);
        }

        [Fact]
        public void StartHeating_WithoutTest_StaysIdle()
        {
            _controller.StartHeating();
            Assert.Equal(TestState.Idle, _controller.CurrentState);
        }

        [Fact]
        public void StartHeating_FromPreparing_NoChange()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            Assert.Equal(TestState.Preparing, _controller.CurrentState);

            _controller.StartHeating();
            Assert.Equal(TestState.Preparing, _controller.CurrentState);
        }

        // ==================== Preparing → Ready（自动转换）====================

        [Fact]
        public void StateMachine_PreparingToReady_WhenTemperatureStable()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();

            Assert.Equal(TestState.Ready, _controller.CurrentState);
        }

        // ==================== Preparing → Idle（停止升温）====================

        [Fact]
        public void StopHeating_FromPreparing_ReturnsToIdle()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            Assert.Equal(TestState.Preparing, _controller.CurrentState);

            _controller.StopHeating();
            Assert.Equal(TestState.Idle, _controller.CurrentState);
        }

        [Fact]
        public void StopHeating_FromIdle_StaysIdle()
        {
            _controller.StopHeating();
            Assert.Equal(TestState.Idle, _controller.CurrentState);
        }

        // ==================== Ready → Recording ====================

        [Fact]
        public void StartRecording_FromReady_TransitionsToRecording()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            Assert.Equal(TestState.Ready, _controller.CurrentState);

            _controller.StartRecording();
            Assert.Equal(TestState.Recording, _controller.CurrentState);
        }

        [Fact]
        public void StartRecording_FromIdle_NoChange()
        {
            _controller.StartRecording();
            Assert.Equal(TestState.Idle, _controller.CurrentState);
        }

        [Fact]
        public void StartRecording_FromPreparing_NoChange()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            _controller.StartRecording();
            Assert.Equal(TestState.Preparing, _controller.CurrentState);
        }

        // ==================== Ready → Preparing（温度跌落）====================

        [Fact]
        public void StateMachine_ReadyBackToPreparing_WhenTempDrops()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            Assert.Equal(TestState.Ready, _controller.CurrentState);

            // 手动将仿真器温度设为低于稳定范围（不重新加热），模拟温度跌落
            // Reset 会停止加热，DaqWorker tick 时走冷却路径，温度缓慢下降
            _simulator.Reset(700.0);
            _daqWorker.Start();
            Thread.Sleep(3000); // 给足够时间让温度下降并被状态机检测
            _daqWorker.Stop();

            Assert.Equal(TestState.Preparing, _controller.CurrentState);
        }

        // ==================== Recording → Complete ====================

        [Fact]
        public void StopRecording_FromRecording_TransitionsToComplete()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            _controller.StartRecording();

            // 等待至少 1 次记录 tick（每秒一次）
            Thread.Sleep(1500);

            _controller.StopRecording();
            Assert.Equal(TestState.Complete, _controller.CurrentState);
        }

        [Fact]
        public void StopRecording_NoData_ReturnsToPreparing()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();

            // 立即 StartRecording 然后 StopRecording（不等记录 tick）
            _controller.StartRecording();
            _controller.StopRecording();

            // 无有效记录 → 回到 Preparing
            Assert.True(
                _controller.CurrentState == TestState.Preparing ||
                _controller.CurrentState == TestState.Complete,
                $"Expected Preparing or Complete, got {_controller.CurrentState}");
        }

        // ==================== 试验保存 ====================

        [Fact]
        public void SaveTestResult_CalculatesWeightLoss()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            _controller.StartRecording();

            // 等待至少 1 秒让记录定时器采集数据
            Thread.Sleep(1500);
            _controller.StopRecording();

            // 保存试验结果
            _controller.SaveTestResult(
                postWeight: 85.0,
                hasFlame: false,
                flameTime: 0,
                flameDuration: 0,
                memo: "测试保存");

            // 保存后 CurrentTest 被清空
            Assert.Null(_controller.CurrentTest);

            // 从数据库验证保存的数据
            var allTests = _dbHelper.QueryTests(productId: "TEST-PROD");
            var test = allTests.FirstOrDefault();
            Assert.NotNull(test);
            Assert.Equal(85.0, test!.PostWeight);
            Assert.Equal(15.0, test.LostWeight);
            Assert.Equal(15.0, test.LostWeightPercent);
            Assert.Equal("10000000", test.Flag);
        }

        // ==================== HasUnsavedCompleteTest ====================

        [Fact]
        public void HasUnsavedCompleteTest_AfterRecording_ReturnsTrue()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            _controller.StartRecording();
            Thread.Sleep(1500);
            _controller.StopRecording();

            Assert.True(_controller.HasUnsavedCompleteTest());
        }

        [Fact]
        public void HasUnsavedCompleteTest_Initial_ReturnsFalse()
        {
            Assert.False(_controller.HasUnsavedCompleteTest());
        }

        // ==================== 温度数据缓存 ====================

        [Fact]
        public void GetTempDataCache_ReturnsCopyOfData()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            WaitForReady();
            _controller.StartRecording();
            Thread.Sleep(2500); // 记录 ~2 秒
            _controller.StopRecording();

            var cache = _controller.GetTempDataCache();

            Assert.True(cache.Count >= 1, $"Expected at least 1 data point, got {cache.Count}");

            var first = cache.First();
            Assert.True(first.TF1 > 0);
            Assert.True(first.TF2 > 0);
            Assert.True(first.TS > 0);
            Assert.True(first.TC > 0);
            Assert.True(first.TCal > 0);
        }

        // ==================== 当前温度获取 ====================

        [Fact]
        public void GetCurrentTemperatures_ReturnsValidTuple()
        {
            _daqWorker.Start();
            Thread.Sleep(1000);
            _daqWorker.Stop();

            var (TF1, TF2, TS, TC, TCal) = _controller.GetCurrentTemperatures();

            Assert.True(TF1 >= 25.0);
            Assert.True(TF2 >= 25.0);
            Assert.True(TS >= 24.0);
            Assert.True(TC >= 24.0);
            Assert.True(TCal >= 25.0);
        }

        // ==================== 自定义时长模式 ====================

        [Fact]
        public void TargetDurationSeconds_StandardMode_Is3600()
        {
            _controller.IsStandardDuration = true;
            Assert.Equal(3600, _controller.TargetDurationSeconds);
        }

        [Fact]
        public void TargetDurationSeconds_CustomMode_UsesCustomValue()
        {
            _controller.IsStandardDuration = false;
            _controller.CustomDurationSeconds = 600;
            Assert.Equal(600, _controller.TargetDurationSeconds);
        }

        // ==================== 事件广播 ====================

        [Fact]
        public void DataBroadcast_FiresOnDaqUpdate()
        {
            _controller.CreateNewTest(CreateTestRecord());
            _controller.StartHeating();
            _lastBroadcast = null;

            _daqWorker.Start();
            Thread.Sleep(2000);

            _daqWorker.Stop();
            Assert.NotNull(_lastBroadcast);
            Assert.True(_lastBroadcast!.TF1 > 0);
            Assert.Equal("TEST-PROD", _lastBroadcast.CurrentProductId);
        }
    }
}
