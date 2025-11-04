using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CleanerControlApp.Modules.UserManagement.Models;

namespace CleanerControlApp.Modules.UserManagement.Services
{
    public class UserManager
    {
        public UserInfo? UserInfo { get; internal set; } = null;

        private string _developer_username = "supervisor";
        private string _developer_password = "9527";

        /// <summary>
        /// Login with username and password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Login(string username, string password)
        {
            bool result = false;

            // if username or password is empty, login fail
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return result;

            if ((username == _developer_username) && (password == _developer_password))
            {
                UserInfo = new UserInfo(0, _developer_username, _developer_password, UserRole.Developer);
                result = true;
            }
            else
            {
                UserInfo = UserRepository.Authenticate(username, password);
                if(UserInfo != null)
                {
                    result = true;
                }
            }

            return result;
        }
    }
}
