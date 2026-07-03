using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        [Display(Name = "记录ID")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>校准日期时间</summary>
        [Display(Name = "校准日期时间")]
        public DateTime CalibrationDate { get; set; }

        /// <summary>校准类型（Surface=表面, Center=中心）</summary>
        [Display(Name = "校准类型")]
        public string CalibrationType { get; set; } = string.Empty;

        /// <summary>设备ID</summary>
        [Display(Name = "设备ID")]
        public int ApparatusId { get; set; }

        /// <summary>操作员</summary>
        [Display(Name = "操作员")]
        public string Operator { get; set; } = string.Empty;

        /// <summary>温度数据（JSON字符串）</summary>
        public string TemperatureData { get; set; } = string.Empty;

        /// <summary>均匀性结果</summary>
        [Display(Name = "均匀性结果")]
        public double? UniformityResult { get; set; }

        /// <summary>最大偏差</summary>
        [Display(Name = "最大偏差")]
        public double? MaxDeviation { get; set; }

        /// <summary>平均温度</summary>
        [Display(Name = "平均温度")]
        public double? AverageTemperature { get; set; }

        /// <summary>是否通过标准（0=否, 1=是）</summary>
        [Display(Name = "是否通过标准")]
        public int PassedCriteria { get; set; }

        /// <summary>备注</summary>
        [Display(Name = "备注")]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        [Display(Name = "创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ===== 炉壁9测温点（A/B/C层 × 1/2/3轴）=====
        [Display(Name = "A层1轴温度")]
        public double? TempA1 { get; set; }

        [Display(Name = "A层2轴温度")]
        public double? TempA2 { get; set; }

        [Display(Name = "A层3轴温度")]
        public double? TempA3 { get; set; }

        [Display(Name = "B层1轴温度")]
        public double? TempB1 { get; set; }

        [Display(Name = "B层2轴温度")]
        public double? TempB2 { get; set; }

        [Display(Name = "B层3轴温度")]
        public double? TempB3 { get; set; }

        [Display(Name = "C层1轴温度")]
        public double? TempC1 { get; set; }

        [Display(Name = "C层2轴温度")]
        public double? TempC2 { get; set; }

        [Display(Name = "C层3轴温度")]
        public double? TempC3 { get; set; }

        // ===== 计算结果 =====
        [Display(Name = "总平均温度")]
        public double? TAvg { get; set; }

        [Display(Name = "1轴平均温度")]
        public double? TAvgAxis1 { get; set; }

        [Display(Name = "2轴平均温度")]
        public double? TAvgAxis2 { get; set; }

        [Display(Name = "3轴平均温度")]
        public double? TAvgAxis3 { get; set; }

        [Display(Name = "A层平均温度")]
        public double? TAvgLevela { get; set; }

        [Display(Name = "B层平均温度")]
        public double? TAvgLevelb { get; set; }

        [Display(Name = "C层平均温度")]
        public double? TAvgLevelc { get; set; }

        [Display(Name = "1轴偏差")]
        public double? TDevAxis1 { get; set; }

        [Display(Name = "2轴偏差")]
        public double? TDevAxis2 { get; set; }

        [Display(Name = "3轴偏差")]
        public double? TDevAxis3 { get; set; }

        [Display(Name = "A层偏差")]
        public double? TDevLevela { get; set; }

        [Display(Name = "B层偏差")]
        public double? TDevLevelb { get; set; }

        [Display(Name = "C层偏差")]
        public double? TDevLevelc { get; set; }

        [Display(Name = "各轴平均偏差")]
        public double? TAvgDevAxis { get; set; }

        [Display(Name = "各层平均偏差")]
        public double? TAvgDevLevel { get; set; }

        /// <summary>中心轴JSON数据</summary>
        public string? CenterTempData { get; set; }

        /// <summary>备忘录</summary>
        [Display(Name = "备忘录")]
        public string? Memo { get; set; }
    }
}

