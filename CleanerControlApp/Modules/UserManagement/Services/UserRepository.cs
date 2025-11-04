using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CleanerControlApp.Modules.UserManagement.Models;
using Microsoft.Data.Sqlite;


namespace CleanerControlApp.Modules.UserManagement.Services
{
    public static class UserRepository
    {
        private static readonly string _dbFile = "users.db";
        private static readonly string _connectionString = $"Data Source={_dbFile}";

        // 初始化資料庫與表格，並建立預設管理者帳號
        public static void Initialize()
        {
            bool needCreateTable = !File.Exists(_dbFile);

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            if (needCreateTable)
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText =
                    @"CREATE TABLE IF NOT EXISTS Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Name TEXT NOT NULL UNIQUE,
                        Password TEXT NOT NULL,
                        Role INTEGER NOT NULL
                    );";
                cmd.ExecuteNonQuery();
            }

            // 檢查 admin 帳號是否存在，若不存在則新增
            using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users WHERE Name = @name";
            checkCmd.Parameters.AddWithValue("@name", "admin");
            long count = (checkCmd.ExecuteScalar() is long v) ? v : 0;
            if (count == 0)
            {
                using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = "INSERT INTO Users (Name, Password, Role) VALUES (@name, @password, @role)";
                insertCmd.Parameters.AddWithValue("@name", "admin");
                insertCmd.Parameters.AddWithValue("@password", "admin");
                insertCmd.Parameters.AddWithValue("@role", (int)UserRole.Administrator);
                insertCmd.ExecuteNonQuery();
            }
        }

        public static List<UserInfo> GetAllUsers()
        {
            var users = new List<UserInfo>();
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Password, Role FROM Users";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string name = reader.GetString(1);
                string password = reader.GetString(2);
                UserRole role = (UserRole)reader.GetInt32(3);
                users.Add(new UserInfo(id, name, password, role));
            }
            return users;
        }

        public static void AddUser(UserInfo user)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "INSERT INTO Users (Name, Password, Role) VALUES (@name, @password, @role)";
            cmd.Parameters.AddWithValue("@name", user.Name);
            cmd.Parameters.AddWithValue("@password", user.Password);
            cmd.Parameters.AddWithValue("@role", (int)user.CurrentUserRole);
            cmd.ExecuteNonQuery();
        }

        public static UserInfo? Authenticate(string name, string password)
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Id, Name, Password, Role FROM Users WHERE Name=@name AND Password=@password";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@password", password);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                int id = reader.GetInt32(0);
                string uname = reader.GetString(1);
                string pwd = reader.GetString(2);
                UserRole role = (UserRole)reader.GetInt32(3);
                return new UserInfo(id, uname, pwd, role);
            }
            return null;
        }
    }
}
