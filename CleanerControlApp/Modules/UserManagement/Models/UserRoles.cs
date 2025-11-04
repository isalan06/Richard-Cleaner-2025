using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.UserManagement.Models
{
    /// <summary>
    /// 使用者權限類型
    /// </summary>
    public enum UserRole
    {
        Developer = 0,   // 開發者
        Administrator = 1, // 管理者
        Engineer = 2,    // 工程師
        Operator = 3     // 操作員
    }

}
