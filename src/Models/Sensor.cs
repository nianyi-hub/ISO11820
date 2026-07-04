using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Display(Name = "传感器ID")]
        public int SensorId { get; set; }

        /// <summary>传感器代号（如 TF1, TF2）</summary>
        [Display(Name = "传感器代号")]
        public string SensorName { get; set; } = string.Empty;

        /// <summary>显示名称（如 炉温1）</summary>
        [Display(Name = "显示名称")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>传感器分组</summary>
        [Display(Name = "传感器分组")]
        public string SensorGroup { get; set; } = string.Empty;

        /// <summary>单位（如 ℃）</summary>
        [Display(Name = "单位")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>描述</summary>
        [Display(Name = "描述")]
        public string Description { get; set; } = string.Empty;

        /// <summary>状态标记（如 启用/禁用）</summary>
        public string Flag { get; set; } = string.Empty;

        /// <summary>信号零点</summary>
        [Display(Name = "信号零点")]
        public double SignalZero { get; set; }

        /// <summary>信号量程</summary>
        [Display(Name = "信号量程")]
        public double SignalSpan { get; set; }

        /// <summary>输出温度下限</summary>
        [Display(Name = "输出温度下限")]
        public double OutputZero { get; set; }

        /// <summary>输出温度上限</summary>
        [Display(Name = "输出温度上限")]
        public double OutputSpan { get; set; }

        /// <summary>当前输出值（温度）</summary>
        [Display(Name = "当前输出值")]
        public double OutputValue { get; set; }

        /// <summary>当前输入值（原始信号）</summary>
        [Display(Name = "当前输入值")]
        public double InputValue { get; set; }

        /// <summary>信号类型（4=数字量仿真）</summary>
        [Display(Name = "信号类型")]
        public int SignalType { get; set; }
    }
}

