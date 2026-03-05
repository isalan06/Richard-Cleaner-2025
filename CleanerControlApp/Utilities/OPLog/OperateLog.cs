using CleanerControlApp.Modules.UserManagement.Services;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CleanerControlApp.Utilities.Log
{
    /// <summary>
    /// 靜態的操作記錄工具，將運行事件寫入檔案。
    /// 資料結構: 日期(本地時間 yyyy-MM-dd HH:mm:ss),事件名稱, 使用者名稱,事件描述
    /// 每日一個檔案，位於執行目錄的 "OperateLog\yyyyMM" 資料夾內，檔名格式: OperateLog-yyyyMMdd.csv
    /// </summary>
    public static class OperateLog
    {
        private static readonly object _sync = new object();

        /// <summary>
        /// 將一筆事件寫入日誌檔案。
        /// </summary>
        /// <param name="eventName">事件名稱</param>
        /// <param name="userName">使用者名稱</param>
        /// <param name="description">事件描述</param>
        public static void Log(string eventName, string userName, string description)
        {
            try
            {
                string dir = GetLogDirectory();
                Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, $"OperateLog-{DateTime.Now:yyyyMMdd}.csv");

                string timestamp = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                string line = string.Join(",",
                EscapeCsvField(timestamp),
                EscapeCsvField(eventName),
                EscapeCsvField(userName),
                EscapeCsvField(description)
                );

                const string header = "Timestamp,EventName,UserName,Description";

                lock (_sync)
                {
                    // If the file does not exist yet, create it with a header
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, header + Environment.NewLine, Encoding.UTF8);
                    }

                    File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // 日誌寫入失敗不應影響主程序，選擇靜默失敗
            }
        }

        public static void Log(string eventName, string description)
        {
            string? userName = UserManager.CurrentUsername;

            if (userName == null) return;
                
            try
            {
                string dir = GetLogDirectory();
                if (UserManager.CurrentUserRole == Modules.UserManagement.Models.UserRole.Developer) dir = GetLogDirectory_Test();
                Directory.CreateDirectory(dir);

                string filePath = Path.Combine(dir, $"OperateLog-{DateTime.Now:yyyyMMdd}.csv");

                string timestamp = DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
                string line = string.Join(",",
                EscapeCsvField(timestamp),
                EscapeCsvField(eventName),
                EscapeCsvField(userName),
                EscapeCsvField(description)
                );

                const string header = "Timestamp,EventName,UserName,Description";

                lock (_sync)
                {
                    // If the file does not exist yet, create it with a header
                    if (!File.Exists(filePath))
                    {
                        File.WriteAllText(filePath, header + Environment.NewLine, Encoding.UTF8);
                    }

                    File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch
            {
                // 日誌寫入失敗不應影響主程序，選擇靜默失敗
            }
        }

        private static string GetLogDirectory()
        {
            // 使用應用程式的執行目錄下的 OperateLog/yyyyMM 資料夾
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            string monthFolder = DateTime.Now.ToString("yyyyMM");
            return Path.Combine(baseDir, "OperateLog", monthFolder);
        }

        private static string GetLogDirectory_Test()
        {
            // 使用應用程式的執行目錄下的 OperateLog/yyyyMM 資料夾
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            string monthFolder = DateTime.Now.ToString("yyyyMM");
            return Path.Combine(baseDir, "TestLog", monthFolder);
        }

        private static string EscapeCsvField(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // 替換雙引號並在必要時以雙引號包起來
            bool mustQuote = input.Contains(",") || input.Contains('"') || input.Contains('\n') || input.Contains('\r');
            string escaped = input.Replace("\"", "\"\"");
            return mustQuote ? $"\"{escaped}\"" : escaped;
        }

        // New: read entries for a specific date
        public static List<OperateLogEntry> GetEntriesForDate(DateTime date)
        {
            try
            {
                // Try normal OperateLog folder first
                string dir = GetLogDirectoryForDate(date);
                string filePath = Path.Combine(dir, $"OperateLog-{date:yyyyMMdd}.csv");
                if (!File.Exists(filePath))
                {
                    // try test log location
                    string testDir = GetLogDirectoryForDate_Test(date);
                    string testFile = Path.Combine(testDir, $"OperateLog-{date:yyyyMMdd}.csv");
                    if (File.Exists(testFile))
                    {
                        filePath = testFile;
                    }
                    else
                    {
                        return new List<OperateLogEntry>();
                    }
                }

                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                var result = new List<OperateLogEntry>();
                bool first = true;
                foreach (var line in lines)
                {
                    if (first)
                    {
                        first = false; // skip header
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var fields = ParseCsvLine(line);
                    if (fields.Count <4) continue;

                    DateTime ts;
                    if (!DateTime.TryParse(fields[0], out ts)) ts = DateTime.MinValue;

                    result.Add(new OperateLogEntry
                    {
                        Timestamp = ts,
                        EventName = fields[1],
                        UserName = fields[2],
                        Description = fields[3]
                    });
                }
                return result;
            }
            catch
            {
                return new List<OperateLogEntry>();
            }
        }

        private static string GetLogDirectoryForDate(DateTime date)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            string monthFolder = date.ToString("yyyyMM");
            return Path.Combine(baseDir, "OperateLog", monthFolder);
        }

        private static string GetLogDirectoryForDate_Test(DateTime date)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            string monthFolder = date.ToString("yyyyMM");
            return Path.Combine(baseDir, "TestLog", monthFolder);
        }

        // Simple CSV parser that respects quoted fields and escaped quotes
        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            if (line == null) return fields;

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i =0; i < line.Length; i++)
            {
                char c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // Peek next char to see if it's another quote (escaped quote)
                        if (i +1 < line.Length && line[i +1] == '"')
                        {
                            sb.Append('"');
                            i++; // skip escaped quote
                        }
                        else
                        {
                            inQuotes = false; // end quote
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == ',')
                    {
                        fields.Add(sb.ToString());
                        sb.Clear();
                    }
                    else if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            // add last field
            fields.Add(sb.ToString());
            return fields;
        }
    }
}
