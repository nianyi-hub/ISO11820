using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ISO11820System.Models
{
    /// <summary>
    /// 操作员数据模型
    /// </summary>
    public class Operator
    {
        /// <summary>用户ID</summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>用户名（登录用）</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>密码（明文存储）</summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>用户类型：admin=管理员, operator=试验员</summary>
        public string UserType { get; set; } = string.Empty;
    }
}
