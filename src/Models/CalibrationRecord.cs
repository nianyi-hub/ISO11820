using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 设备校准记录数据模型
    /// </summary>
    public class CalibrationRecord
    {
        /// <summary>记录ID（GUID）</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>校准日期时间</summary>
        public DateTime CalibrationDate { get; set; }

        /// <summary>校准类型（Surface=表面, Center=中心）</summary>
        public string CalibrationType { get; set; } = string.Empty;

        /// <summary>设备ID</summary>
        public int ApparatusId { get; set; }

        /// <summary>操作员</summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>温度数据（JSON字符串）</summary>
        public string TemperatureData { get; set; } = string.Empty;

        /// <summary>均匀性结果</summary>
        public double? UniformityResult { get; set; }

        /// <summary>最大偏差</summary>
        public double? MaxDeviation { get; set; }

        /// <summary>平均温度</summary>
        public double? AverageTemperature { get; set; }

        /// <summary>是否通过标准（0=否, 1=是）</summary>
        public int PassedCriteria { get; set; }

        /// <summary>备注</summary>
        public string Remarks { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===== 炉壁9测温点（A/B/C层 × 1/2/3轴）=====
        public double? TempA1 { get; set; }
        public double? TempA2 { get; set; }
        public double? TempA3 { get; set; }
        public double? TempB1 { get; set; }
        public double? TempB2 { get; set; }
        public double? TempB3 { get; set; }
        public double? TempC1 { get; set; }
        public double? TempC2 { get; set; }
        public double? TempC3 { get; set; }

        // ===== 计算结果 =====
        public double? TAvg { get; set; }
        public double? TAvgAxis1 { get; set; }
        public double? TAvgAxis2 { get; set; }
        public double? TAvgAxis3 { get; set; }
        public double? TAvgLevela { get; set; }
        public double? TAvgLevelb { get; set; }
        public double? TAvgLevelc { get; set; }
        public double? TDevAxis1 { get; set; }
        public double? TDevAxis2 { get; set; }
        public double? TDevAxis3 { get; set; }
        public double? TDevLevela { get; set; }
        public double? TDevLevelb { get; set; }
        public double? TDevLevelc { get; set; }
        public double? TAvgDevAxis { get; set; }
        public double? TAvgDevLevel { get; set; }

        /// <summary>中心轴JSON数据</summary>
        public string? CenterTempData { get; set; }

        /// <summary>备忘录</summary>
        public string? Memo { get; set; }
    }
}
