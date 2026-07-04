using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ISO11820System.Models;

namespace ISO11820System.Services
{
    /// <summary>
    /// 温度传感器仿真引擎
    /// </summary>
    public class SensorSimulator
    {
        private readonly Random _random = new();

        // 配置参数
        public double TargetTemp { get; set; } = 750.0;
        public double HeatingRate { get; set; } = 40.0; // 升温速度（°C/s）
        public double TempFluctuation { get; set; } = 0.5;
        public double StableThreshold { get; set; } = 3.0;
        public double SurfaceFollowRatio { get; set; } = 0.3;
        public double CenterFollowRatio { get; set; } = 0.25;
        public double SurfaceApproachRate { get; set; } = 0.02;
        public double CenterApproachRate { get; set; } = 0.01;

        // 当前温度值
        public double TF1 { get; private set; } = 25.0;
        public double TF2 { get; private set; } = 25.0;
        public double TS { get; private set; } = 25.0;
        public double TC { get; private set; } = 25.0;
        public double TCal { get; private set; } = 25.0;

        // 状态
        private bool _isHeating = false;
        private int _stableCounter = 0;
        public bool IsStable { get; private set; } = false;

        // 记录状态
        private bool _isRecording = false;

        // PID输出值队列（用于计算恒功率，最多600个）
        private readonly Queue<double> _pidOutputQueue = new();
        private const int MAX_PID_QUEUE_SIZE = 600;

        /// <summary>
        /// 获取PID输出队列的平均值（恒功率）
        /// </summary>
        public double GetAveragePidOutput()
        {
            if (_pidOutputQueue.Count == 0) return 2048;
            return _pidOutputQueue.Average();
        }

        /// <summary>
        /// 开始加热
        /// </summary>
        public void StartHeating()
        {
            _isHeating = true;
            _stableCounter = 0;
            IsStable = false;
            _pidOutputQueue.Clear();
        }

        /// <summary>
        /// 停止加热
        /// </summary>
        public void StopHeating()
        {
            _isHeating = false;
            _stableCounter = 0;
            IsStable = false;
            _pidOutputQueue.Clear();
        }

        /// <summary>
        /// 开始记录（切换到恒温模式）
        /// </summary>
        public void StartRecording()
        {
            _isRecording = true;
        }

        /// <summary>
        /// 停止记录
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
        }

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        public void Reset(double initialTemp = 25.0)
        {
            TF1 = TF2 = TS = TC = TCal = initialTemp;
            _isHeating = false;
            _isRecording = false;
            _stableCounter = 0;
            IsStable = false;
            _pidOutputQueue.Clear();
        }

        /// <summary>
        /// 更新温度（每800ms调用一次）
        /// </summary>
        public void Update(double deltaTime = 0.8)
        {
            if (_isHeating)
            {
                // 升温阶段
                if (TF1 < TargetTemp - StableThreshold)
                {
                    // 快速升温（不超过目标温度，防止冲过头）
                    TF1 = Math.Min(TF1 + HeatingRate * deltaTime + GetNoise(), TargetTemp);
                    TF2 = Math.Min(TF2 + HeatingRate * deltaTime + GetNoise(), TargetTemp);

                    // 表面温和中心温在非记录阶段跟随较慢
                    if (!_isRecording)
                    {
                        TS = TF1 * SurfaceFollowRatio + GetNoise();
                        TC = TF1 * CenterFollowRatio + GetNoise();
                    }
                }
                else
                {
                    // 稳定阶段：钳位到目标温度
                    TF1 = TargetTemp + GetNoise();
                    TF2 = TargetTemp + GetNoise();

                    // 稳定计数器
                    _stableCounter++;
                    if (_stableCounter > 3)
                    {
                        IsStable = true;
                    }
                }

                // 模拟PID输出值（用于恒功率计算）
                double pidOutput = 2048 + GetNoise() * 50;
                _pidOutputQueue.Enqueue(pidOutput);
                if (_pidOutputQueue.Count > MAX_PID_QUEUE_SIZE)
                    _pidOutputQueue.Dequeue();

                // 记录阶段：表面温和中心温指数接近目标值
                if (_isRecording)
                {
                    double surfaceTarget = Math.Min(TF1 * 0.95, 800);
                    TS += (surfaceTarget - TS) * SurfaceApproachRate + GetNoise();

                    double centerTarget = Math.Min(TF1 * 0.85, 750);
                    TC += (centerTarget - TC) * CenterApproachRate + GetNoise();
                }

                // 校准温度
                TCal = TF1 + GetNoise() * 2;
            }
            else
            {
                // 停止加热后缓慢降温
                if (TF1 > 25)
                {
                    TF1 -= 0.5 + Math.Abs(GetNoise()) * 0.1;
                    TF2 -= 0.5 + Math.Abs(GetNoise()) * 0.1;
                    TS -= 0.3;
                    TC -= 0.2;
                    TCal = TF1 + GetNoise() * 2;
                }
                IsStable = false;
                _stableCounter = 0;
            }

            // 确保温度不低于室温
            TF1 = Math.Max(TF1, 25);
            TF2 = Math.Max(TF2, 25);
            TS = Math.Max(TS, 24);
            TC = Math.Max(TC, 24);
            TCal = Math.Max(TCal, 25);
        }

        /// <summary>
        /// 获取随机噪声
        /// </summary>
        private double GetNoise()
        {
            return (_random.NextDouble() * 2 - 1) * TempFluctuation;
        }

        /// <summary>
        /// 检查是否达到就绪条件（745~755°C且稳定）
        /// </summary>
        public bool CheckReadyCriteria()
        {
            return TF1 >= 745 && TF1 <= 755 && IsStable;
        }
    }
}
