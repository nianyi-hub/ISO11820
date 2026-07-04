using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 试验状态枚举
    /// </summary>
    public enum TestState
    {
        /// <summary>空闲状态 - 未开始试验</summary>
        Idle = 0,

        /// <summary>升温中 - 炉温正在上升到目标温度</summary>
        Preparing = 1,

        /// <summary>就绪 - 温度已稳定，可以开始记录</summary>
        Ready = 2,

        /// <summary>记录中 - 正在记录试验数据</summary>
        Recording = 3,

        /// <summary>完成 - 试验已完成</summary>
        Complete = 4
    }
}
