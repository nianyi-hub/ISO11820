using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 试验设备数据模型
    /// </summary>
    public class Apparatus
    {
        /// <summary>设备ID（主键）</summary>
        [Display(Name = "设备ID")]
        public int ApparatusId { get; set; }

        /// <summary>设备内部编号</summary>
        [Display(Name = "设备内部编号")]
        public string InnerNumber { get; set; } = string.Empty;

        /// <summary>设备名称</summary>
        [Display(Name = "设备名称")]
        public string ApparatusName { get; set; } = string.Empty;

        /// <summary>检定有效期开始日期</summary>
        [Display(Name = "检定有效期开始日期")]
        public DateTime CheckDateFrom { get; set; }

        /// <summary>检定有效期结束日期</summary>
        [Display(Name = "检定有效期结束日期")]
        public DateTime CheckDateTo { get; set; }

        /// <summary>PID控制器串口</summary>
        [Display(Name = "PID控制器串口")]
        public string PidPort { get; set; } = string.Empty;

        /// <summary>功率控制串口</summary>
        [Display(Name = "功率控制串口")]
        public string PowerPort { get; set; } = string.Empty;

        /// <summary>恒功率值（上次记录的）</summary>
        [Display(Name = "恒功率值")]
        public int? ConstPower { get; set; }
    }
}
