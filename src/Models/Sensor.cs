using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 传感器配置数据模型
    /// </summary>
    public class Sensor
    {
        /// <summary>传感器ID（主键）</summary>
        public int SensorId { get; set; }

        /// <summary>传感器代号（如 TF1, TF2）</summary>
        public string SensorName { get; set; } = string.Empty;

        /// <summary>显示名称（如 炉温1）</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>传感器分组</summary>
        public string SensorGroup { get; set; } = string.Empty;

        /// <summary>单位（如 ℃）</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>描述</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>状态标记（如 启用/禁用）</summary>
        public string Flag { get; set; } = string.Empty;

        /// <summary>信号零点</summary>
        public double SignalZero { get; set; }

        /// <summary>信号量程</summary>
        public double SignalSpan { get; set; }

        /// <summary>输出温度下限</summary>
        public double OutputZero { get; set; }

        /// <summary>输出温度上限</summary>
        public double OutputSpan { get; set; }

        /// <summary>当前输出值（温度）</summary>
        public double OutputValue { get; set; }

        /// <summary>当前输入值（原始信号）</summary>
        public double InputValue { get; set; }

        /// <summary>信号类型（4=数字量仿真）</summary>
        public int SignalType { get; set; }
    }
}

