using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 试验记录数据模型（核心表）
    /// </summary>
    public class TestMaster
    {
        // ===== 基本信息 =====
        /// <summary>样品编号（联合主键）</summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>试验ID（联合主键，格式：yyyyMMdd-HHmmss）</summary>
        public string TestId { get; set; } = string.Empty;

        /// <summary>试验日期</summary>
        public DateTime TestDate { get; set; }

        /// <summary>环境温度（°C）</summary>
        public double AmbientTemp { get; set; }

        /// <summary>环境湿度（%）</summary>
        public double AmbientHumidity { get; set; }

        /// <summary>试验依据（如 ISO 11820:2022）</summary>
        public string According { get; set; } = "ISO 11820:2022";

        /// <summary>操作员用户名</summary>
        public string Operator { get; set; } = string.Empty;

        /// <summary>设备编号</summary>
        public string ApparatusId { get; set; } = string.Empty;

        /// <summary>设备名称</summary>
        public string ApparatusName { get; set; } = string.Empty;

        /// <summary>设备检定日期</summary>
        public DateTime ApparatusCheckDate { get; set; }

        /// <summary>报告编号</summary>
        public string ReportNo { get; set; } = string.Empty;

        // ===== 质量数据 =====
        /// <summary>试验前质量（g）</summary>
        public double PreWeight { get; set; }

        /// <summary>试验后质量（g）</summary>
        public double PostWeight { get; set; }

        /// <summary>失重量（g）</summary>
        public double LostWeight { get; set; }

        /// <summary>失重率（%）- 判定项</summary>
        public double LostWeightPercent { get; set; }

        // ===== 试验过程 =====
        /// <summary>总试验时长（秒）</summary>
        public int TotalTestTime { get; set; }

        /// <summary>恒功率值</summary>
        public int ConstPower { get; set; }

        /// <summary>现象编码（序列化字符串）</summary>
        public string PhenoCode { get; set; } = string.Empty;

        /// <summary>火焰开始时刻（秒，无火焰填0）</summary>
        public int FlameTime { get; set; }

        /// <summary>火焰持续时间（秒，无火焰填0）</summary>
        public int FlameDuration { get; set; }

        // ===== 各通道温度最大值 =====
        /// <summary>炉温1最大值（°C）</summary>
        public double MaxTF1 { get; set; }
        public double MaxTF2 { get; set; }
        public double MaxTS { get; set; }
        public double MaxTC { get; set; }

        /// <summary>各通道最大值时刻（秒）</summary>
        public int MaxTF1Time { get; set; }
        public int MaxTF2Time { get; set; }
        public int MaxTSTime { get; set; }
        public int MaxTCTime { get; set; }

        // ===== 各通道温度最终值 =====
        /// <summary>炉温1最终值（°C）</summary>
        public double FinalTF1 { get; set; }
        public double FinalTF2 { get; set; }
        public double FinalTS { get; set; }
        public double FinalTC { get; set; }

        /// <summary>各通道最终值时刻（秒）</summary>
        public int FinalTF1Time { get; set; }
        public int FinalTF2Time { get; set; }
        public int FinalTSTime { get; set; }
        public int FinalTCTime { get; set; }

        // ===== 温升数据 =====
        /// <summary>炉温1温升（°C）</summary>
        public double DeltaTF1 { get; set; }
        public double DeltaTF2 { get; set; }

        /// <summary>样品温升（°C）- 判定项</summary>
        public double DeltaTF { get; set; }

        public double DeltaTS { get; set; }
        public double DeltaTC { get; set; }

        // ===== 其他 =====
        /// <summary>备注</summary>
        public string? Memo { get; set; }

        /// <summary>标记字段（10000000=已完成）</summary>
        public string? Flag { get; set; }
    }
}
