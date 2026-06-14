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

        // Static variables to hold current logged-in username and role
        public static string? CurrentUsername = null;
        public static UserRole? CurrentUserRole = UserRole.Operator;

        private string _developer_username = "supervisor";
        private string _developer_password = "9527";

        private string _developer_username2 = "Richard.Lee";
        private string _developer_password2 = "8748";

        private string _developer_username3 = "@@";
        private string _developer_password3 = "@@";

        private static bool _login = false;
        public static bool IsLogin => _login;
        public static bool CanPassCheck => !_login || CurrentUserRole == UserRole.Developer;

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
            {
                // clear static values on failed login
                CurrentUsername = null;
                CurrentUserRole = UserRole.Operator;
                return result;
            }

            if (((username == _developer_username) && (password == _developer_password)) ||
                ((username == _developer_username2) && (password == _developer_password2)) ||
                ((username == _developer_username3) && (password == _developer_password3)))
            {
                UserInfo = new UserInfo(0, _developer_username, _developer_password, UserRole.Developer);
                result = true;

                // set static values
                CurrentUsername = _developer_username;
                CurrentUserRole = UserRole.Developer;
            }
            else
            {
                UserInfo = UserRepository.Authenticate(username, password);
                if(UserInfo != null)
                {
                    result = true;

                    // set static values from authenticated user
                    CurrentUsername = UserInfo.Name;
                    CurrentUserRole = UserInfo.CurrentUserRole;
                }
                else
                {
                    // clear static values on failed login
                    CurrentUsername = null;
                    CurrentUserRole = null;
                }
            }

            _login = result;

            return result;
        }
    }
}
