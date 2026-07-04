using ISO11820System.Services;

namespace ISO11820System.Tests
{
    /// <summary>
    /// 温度传感器仿真引擎 — 单元测试
    /// 覆盖：温度更新算法、状态转换、边界条件
    /// </summary>
    public class SensorSimulatorTests
    {
        // ==================== 初始化与重置 ====================

        [Fact]
        public void Constructor_DefaultValues_AllTempsAtRoomTemperature()
        {
            var sim = new SensorSimulator();

            Assert.Equal(25.0, sim.TF1);
            Assert.Equal(25.0, sim.TF2);
            Assert.Equal(25.0, sim.TS);
            Assert.Equal(25.0, sim.TC);
            Assert.Equal(25.0, sim.TCal);
            Assert.False(sim.IsStable);
        }

        [Fact]
        public void Reset_SetsAllTempsToInitialValue()
        {
            var sim = new SensorSimulator();
            sim.StartHeating();
            // 多轮更新使温度显著升高
            for (int i = 0; i < 50; i++) sim.Update(0.8);

            sim.Reset(30.0);

            Assert.Equal(30.0, sim.TF1);
            Assert.Equal(30.0, sim.TF2);
            Assert.False(sim.IsStable);
        }

        // ==================== 升温逻辑 ====================

        [Fact]
        public void StartHeating_EnablesHeatingMode()
        {
            var sim = new SensorSimulator();
            sim.StartHeating();
            sim.Update(0.8);

            // 升温后 TF1 应高于初始值（25°C）
            Assert.True(sim.TF1 > 25.0, $"Expected TF1 > 25, got {sim.TF1}");
        }

        [Fact]
        public void Update_DuringHeating_TemperatureIncreases()
        {
            var sim = new SensorSimulator { HeatingRate = 10.0, TempFluctuation = 0.0 };
            sim.StartHeating();

            double beforeTF1 = sim.TF1;
            sim.Update(0.8);

            // 无噪声时，应精确增加 HeatingRate * deltaTime
            Assert.True(sim.TF1 > beforeTF1, $"TF1 should increase: {beforeTF1} -> {sim.TF1}");
        }

        [Fact]
        public void Update_HeatingRate_AffectsTemperatureRise()
        {
            // 使用高升温速率
            var simFast = new SensorSimulator { HeatingRate = 100.0, TempFluctuation = 0.0 };
            simFast.StartHeating();
            simFast.Update(0.8);
            double fastRise = simFast.TF1 - 25.0;

            // 使用低升温速率
            var simSlow = new SensorSimulator { HeatingRate = 10.0, TempFluctuation = 0.0 };
            simSlow.StartHeating();
            simSlow.Update(0.8);
            double slowRise = simSlow.TF1 - 25.0;

            Assert.True(fastRise > slowRise,
                $"Fast rise ({fastRise:F2}) should exceed slow rise ({slowRise:F2})");
        }

        [Fact]
        public void Update_AtTargetTemp_ClampsToTarget()
        {
            var sim = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0, // 极高速率
                TempFluctuation = 0.0  // 无噪声，确保钳位
            };
            sim.StartHeating();

            // 一次更新就会超过目标
            sim.Update(0.8);

            // 由于钳位逻辑，TF1 不应超过 TargetTemp
            Assert.True(sim.TF1 <= 750.0, $"TF1 ({sim.TF1}) should not exceed target 750");
        }

        [Fact]
        public void Update_StablePhase_BecomesStableAfterEnoughTicks()
        {
            var sim = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0, // 瞬间到达目标
                TempFluctuation = 0.0
            };
            sim.StartHeating();

            // 第一次 tick：到达目标
            sim.Update(0.8);
            Assert.False(sim.IsStable, "Should not be stable after 1 tick");

            // 再 tick 几次（需 >3 次稳定 tick）
            sim.Update(0.8);
            sim.Update(0.8);
            sim.Update(0.8); // 第4次
            sim.Update(0.8); // 第5次 — 此时 stableCounter > 3

            Assert.True(sim.IsStable, "Should be stable after >3 ticks at target");
        }

        [Fact]
        public void CheckReadyCriteria_TrueOnlyWhenStableAndInRange()
        {
            var sim = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0,
                TempFluctuation = 0.0
            };
            sim.StartHeating();

            // 未稳定前
            sim.Update(0.8);
            Assert.False(sim.CheckReadyCriteria());

            // 稳定后（再 tick 4次）
            for (int i = 0; i < 4; i++) sim.Update(0.8);
            Assert.True(sim.CheckReadyCriteria());
        }

        // ==================== 降温逻辑 ====================

        [Fact]
        public void StopHeating_DisablesHeatingMode()
        {
            var sim = new SensorSimulator { HeatingRate = 40.0 };
            sim.StartHeating();
            sim.Update(0.8);
            Assert.True(sim.TF1 > 25.0);

            sim.StopHeating();
            double beforeCool = sim.TF1;
            sim.Update(0.8);

            // 降温后温度应低于之前
            Assert.True(sim.TF1 < beforeCool,
                $"TF1 should decrease after stopping: {beforeCool:F2} -> {sim.TF1:F2}");
        }

        [Fact]
        public void Update_Cooling_NeverBelowRoomTemp()
        {
            var sim = new SensorSimulator { TempFluctuation = 0.0 };
            // 不加热，直接冷却
            sim.Update(0.8);
            sim.Update(0.8);
            sim.Update(0.8);

            Assert.True(sim.TF1 >= 25.0, $"TF1 ({sim.TF1}) should not go below 25°C");
            Assert.True(sim.TF2 >= 25.0);
            Assert.True(sim.TS >= 24.0);
            Assert.True(sim.TC >= 24.0);
            Assert.True(sim.TCal >= 25.0);
        }

        [Fact]
        public void StopHeating_ResetsStableFlag()
        {
            var sim = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0,
                TempFluctuation = 0.0
            };
            sim.StartHeating();
            for (int i = 0; i < 5; i++) sim.Update(0.8);
            Assert.True(sim.IsStable);

            sim.StopHeating();
            Assert.False(sim.IsStable);
        }

        // ==================== 记录模式 ====================

        [Fact]
        public void StartRecording_EnablesRecordingMode()
        {
            var sim = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 40.0
            };
            sim.StartHeating();
            sim.StartRecording();

            // 记录模式下加热到目标温度
            for (int i = 0; i < 100; i++) sim.Update(0.8);

            // 表面温和中心温应趋近炉温（记录模式下 TS ≈ TF1*0.95, TC ≈ TF1*0.85）
            Assert.True(sim.TS > 25.0, $"TS ({sim.TS:F1}) should rise in recording mode");
            Assert.True(sim.TC > 25.0, $"TC ({sim.TC:F1}) should rise in recording mode");
        }

        [Fact]
        public void StopRecording_DisablesRecordingMode()
        {
            var sim = new SensorSimulator();
            sim.StartRecording();
            sim.StopRecording();
            // 不应抛出异常，状态应正确切换
            sim.Update(0.8); // 非记录模式下的更新
        }

        // ==================== PID 输出队列 ====================

        [Fact]
        public void GetAveragePidOutput_Empty_ReturnsDefault()
        {
            var sim = new SensorSimulator();
            double avg = sim.GetAveragePidOutput();
            Assert.Equal(2048, avg);
        }

        [Fact]
        public void GetAveragePidOutput_AfterHeating_ReturnsReasonableValue()
        {
            var sim = new SensorSimulator { TempFluctuation = 0.0 };
            sim.StartHeating();
            for (int i = 0; i < 10; i++) sim.Update(0.8);

            double avg = sim.GetAveragePidOutput();
            // 无噪声时 pidOutput = 2048，平均值也是 2048
            Assert.Equal(2048, avg);
        }

        // ==================== 温度通道行为 ====================

        [Fact]
        public void Update_Heating_CalibrationTempFollowsTF1()
        {
            var sim = new SensorSimulator { TempFluctuation = 0.0 };
            sim.StartHeating();
            sim.Update(0.8);

            // 无噪声时 TCal = TF1
            Assert.Equal(sim.TF1, sim.TCal, 1);
        }

        [Fact]
        public void Update_NotRecording_SurfaceAndCenterLowFollow()
        {
            var sim = new SensorSimulator { TempFluctuation = 0.0 };
            sim.StartHeating();

            // 多次升温让温度足够高，使 TS/TC 脱离 24°C 的最低钳位
            for (int i = 0; i < 10; i++) sim.Update(0.8);

            // 非记录阶段：TS = TF1*0.3, TC = TF1*0.25（无噪声时）
            Assert.True(sim.TS < sim.TF1, $"TS ({sim.TS:F1}) should be lower than TF1 ({sim.TF1:F1})");
            Assert.True(sim.TC < sim.TS, $"TC ({sim.TC:F1}) should be lower than TS ({sim.TS:F1})");
        }

        // ==================== 配置参数 ====================

        [Fact]
        public void TempFluctuation_ProducesVariation()
        {
            var sim = new SensorSimulator
            {
                HeatingRate = 10.0,
                TempFluctuation = 1.0 // 大幅波动
            };
            sim.StartHeating();

            var temps = new List<double>();
            for (int i = 0; i < 20; i++)
            {
                sim.Update(0.8);
                temps.Add(sim.TF1);
            }

            // 有噪声时，温度不应全部相同
            bool allSame = temps.All(t => Math.Abs(t - temps[0]) < 0.001);
            Assert.False(allSame, "Temperatures should vary with fluctuation > 0");
        }

        [Fact]
        public void StableThreshold_DeterminesWhenToStabilize()
        {
            // 设置大阈值：更容易进入稳定
            var simWide = new SensorSimulator
            {
                TargetTemp = 750.0,
                HeatingRate = 1000.0,
                StableThreshold = 100.0, // 很宽的阈值
                TempFluctuation = 0.0
            };
            simWide.StartHeating();
            simWide.Update(0.8);
            // 从 25°C 升到 750°C，仍在 100°C 阈值内
            // TF1 会被 Math.Min(25 + 800, 750) = 750 钳位
            Assert.True(simWide.TF1 >= 650, $"With wide threshold, TF1 should reach target area: {simWide.TF1}");
        }
    }
}
