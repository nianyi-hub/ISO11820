using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 样品信息数据模型
    /// </summary>
    public class ProductMaster
    {
        /// <summary>样品编号（主键）</summary>
        public string ProductId { get; set; } = string.Empty;

        /// <summary>样品名称</summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>规格型号</summary>
        public string Specific { get; set; } = string.Empty;

        /// <summary>直径（mm）</summary>
        public double Diameter { get; set; }

        /// <summary>高度（mm）</summary>
        public double Height { get; set; }

        /// <summary>备用标记字段</summary>
        public string? Flag { get; set; }
    }
}