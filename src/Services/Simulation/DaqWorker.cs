using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ISO11820System.Models;

namespace ISO11820System.Services
{
    /// <summary>
    /// 数据采集服务（定时器 + 多线程）
    /// 支持仿真模式和硬件模式切换
    /// </summary>
    public class DaqWorker
    {
        private readonly SensorSimulator _simulator;
        private readonly bool _enableSimulation;
        private System.Threading.Timer? _timer;
        private bool _isRunning = false;
        private int _tickCount = 0;

        // 事件：数据更新
        public event EventHandler<(double TF1, double TF2, double TS, double TC, double TCal)>? DataUpdated;

        public DaqWorker(SensorSimulator simulator, bool enableSimulation = true)
        {
            _simulator = simulator;
            _enableSimulation = enableSimulation;
        }

        /// <summary>
        /// 获取已运行的tick数
        /// </summary>
        public int TickCount => _tickCount;

        /// <summary>
        /// 启动数据采集
        /// </summary>
        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;
            _tickCount = 0;
            _timer = new System.Threading.Timer(DoWork, null, 0, 800); // 每800ms执行一次
        }

        /// <summary>
        /// 停止数据采集
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _timer?.Dispose();
            _timer = null;
        }

        /// <summary>
        /// 定时器回调（后台线程）
        /// </summary>
        private void DoWork(object? state)
        {
            if (!_isRunning) return;

            if (_enableSimulation)
            {
                // 仿真模式：调用仿真引擎生成温度数据
                _simulator.Update(0.8);
            }
            else
            {
                // 硬件模式：通过串口Modbus读取真实传感器（暂未实现）
                // TODO: 实现Modbus RTU通信
            }

            _tickCount++;

            // 触发数据更新事件
            DataUpdated?.Invoke(this, (_simulator.TF1, _simulator.TF2, _simulator.TS, _simulator.TC, _simulator.TCal));
        }

        /// <summary>
        /// 获取当前温度
        /// </summary>
        public (double TF1, double TF2, double TS, double TC, double TCal) GetCurrentTemp()
        {
            return (_simulator.TF1, _simulator.TF2, _simulator.TS, _simulator.TC, _simulator.TCal);
        }
    }
}
