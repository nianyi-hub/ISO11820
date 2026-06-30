using System;
using System.Collections.Generic;
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
        public int ApparatusId { get; set; }

        /// <summary>设备内部编号</summary>
        public string InnerNumber { get; set; } = string.Empty;

        /// <summary>设备名称</summary>
        public string ApparatusName { get; set; } = string.Empty;

        /// <summary>检定有效期开始日期</summary>
        public DateTime CheckDateFrom { get; set; }

        /// <summary>检定有效期结束日期</summary>
        public DateTime CheckDateTo { get; set; }

        /// <summary>PID控制器串口</summary>
        public string PidPort { get; set; } = string.Empty;

        /// <summary>功率控制串口</summary>
        public string PowerPort { get; set; } = string.Empty;

        /// <summary>恒功率值（上次记录的）</summary>
        public int? ConstPower { get; set; }
    }
}
