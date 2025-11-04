using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Modules.UserManagement.Models
{
    public class UserInfo
    {
        private int _id;
        private string _name;
        private string _password;
        private UserRole _role;

        public int Id { get { return _id; } }
        public string Name { get { return _name; } }
        public string Password { get { return _password; } }
        public UserRole CurrentUserRole { get { return _role; } }

        public UserInfo(int id, string name, string password, UserRole role)
        {
            _id = id;
            _name = name;
            _password = password;
            _role = role;

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public UserInfo Clone() 
        {
            return new UserInfo(this._id, this._name, this._password, this._role);
        }
    }
}
