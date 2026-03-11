using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CleanerControlApp.Utilities.Alarm
{
    /// <summary>
    /// Alarm category used to distinguish warnings from alarms.
    /// </summary>
    public enum AlarmType
    {
        Warning,
        Alarm
    }

    /// <summary>
    /// Represents a single alarm entry.
    /// </summary>
    // Reordered parameters: Code, Type, Module, Description, Solution
    public sealed record AlarmInfo(string Code, AlarmType Type, string Module, string Description, string Solution);

    /// <summary>
    /// Provides a static list of alarms with lookup helpers.
    /// Loads from external AlarmMessage.csv in application base directory if present; otherwise writes built-in defaults to that file.
    /// </summary>
    public static class AlarmList
    {
        // Predefined alarms. Add or modify entries here.
        private static readonly Dictionary<string, AlarmInfo> _alarms = new(StringComparer.OrdinalIgnoreCase)
        {
            { "ALM401", new AlarmInfo("ALM401", AlarmType.Warning, "烘乾槽#1", "到達低溫逾時", "停機並檢查冷卻與電流") },
            { "ALM402", new AlarmInfo("ALM402", AlarmType.Warning, "烘乾槽#1", "到達高溫逾時", "檢查設定與溫度感測元件") },
            { "ALM403", new AlarmInfo("ALM403", AlarmType.Warning, "烘乾槽#1", "上蓋打開逾時", "確認蓋子/感測器狀態\r\n請在檢查") },
            { "ALM404", new AlarmInfo("ALM404", AlarmType.Warning, "烘乾槽#1", "上蓋關閉逾時", "確認蓋子/感測器狀態") },
            { "ALM501", new AlarmInfo("ALM501", AlarmType.Warning, "烘乾槽#2", "到達低溫逾時", "停機並檢查冷卻與電流") },
            { "ALM502", new AlarmInfo("ALM502", AlarmType.Warning, "烘乾槽#2", "到達高溫逾時", "檢查設定與溫度感測元件") },
            { "ALM503", new AlarmInfo("ALM503", AlarmType.Warning, "烘乾槽#2", "上蓋打開逾時", "確認蓋子/感測器狀態") },
            { "ALM504", new AlarmInfo("ALM504", AlarmType.Warning, "烘乾槽#2", "上蓋關閉逾時", "確認蓋子/感測器狀態") }
        };

        private static readonly object _fileLock = new();

        static AlarmList()
        {
            try
            {
                LoadFromFile();
            }
            catch
            {
                // Ignore errors - fallback to built-in defaults
            }
        }

        // Extracted file loading logic so it can be retried at runtime
        private static void LoadFromFile()
        {
            lock (_fileLock)
            {
                var baseDir = AppContext.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
                var file = Path.Combine(baseDir, "AlarmMessage.csv");

                if (!File.Exists(file))
                {
                    // write current defaults to file for editing
                    var sbDef = new StringBuilder();
                    sbDef.AppendLine("Code,Type,Module,Description,Solution");
                    foreach (var kv in _alarms.Values)
                    {
                        sbDef.AppendLine(string.Join(",", EscapeCsv(kv.Code), EscapeCsv(kv.Type.ToString()), EscapeCsv(kv.Module), EscapeCsv(kv.Description), EscapeCsv(kv.Solution)));
                    }

                    File.WriteAllText(file, sbDef.ToString(), Encoding.UTF8);
                    return;
                }

                var content = File.ReadAllText(file, Encoding.UTF8);
                var records = ParseCsvRecords(content);
                var entries = new Dictionary<string, AlarmInfo>(StringComparer.OrdinalIgnoreCase);

                // Expect header at first record, skip it
                foreach (var fields in records.Skip(1))
                {
                    if (fields == null || fields.Length ==0)
                        continue;

                    var code = fields.ElementAtOrDefault(0)?.Trim() ?? string.Empty;
                    if (string.IsNullOrEmpty(code))
                        continue;

                    var typeStr = fields.ElementAtOrDefault(1)?.Trim() ?? string.Empty;
                    if (!Enum.TryParse<AlarmType>(typeStr, true, out var type))
                        type = AlarmType.Warning;

                    var module = fields.ElementAtOrDefault(2) ?? string.Empty;
                    var desc = fields.ElementAtOrDefault(3) ?? string.Empty;
                    var solution = fields.ElementAtOrDefault(4) ?? string.Empty;

                    entries[code] = new AlarmInfo(code, type, module, desc, solution);
                }

                if (entries.Count >0)
                {
                    _alarms.Clear();
                    foreach (var kv in entries)
                        _alarms[kv.Key] = kv.Value;
                }
            }
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
                return string.Empty;
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');
            var v = value.Replace("\"", "\"\"");
            if (needsQuotes)
                return "\"" + v + "\"";
            return v;
        }

        private static List<string[]> ParseCsvRecords(string content)
        {
            var records = new List<string[]>();
            if (string.IsNullOrEmpty(content))
                return records;

            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i =0; i < content.Length; i++)
            {
                var c = content[i];
                if (c == '"')
                {
                    if (inQuotes && i +1 < content.Length && content[i +1] == '"')
                    {
                        // Escaped quote
                        sb.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else if ((c == '\r' || c == '\n') && !inQuotes)
                {
                    // handle CRLF or LF
                    if (c == '\r' && i +1 < content.Length && content[i +1] == '\n')
                        i++; // skip LF

                    fields.Add(sb.ToString());
                    sb.Clear();
                    records.Add(fields.ToArray());
                    fields = new List<string>();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // final field/record
            if (inQuotes)
            {
                // unterminated quote - still try to proceed
            }

            // append last field
            fields.Add(sb.ToString());
            // if there is any field or last record not empty, add
            if (fields.Count >1 || (fields.Count ==1 && !string.IsNullOrEmpty(fields[0])))
                records.Add(fields.ToArray());

            return records;
        }

        private static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            if (line == null)
                return result.ToArray();

            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i =0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i +1 < line.Length && line[i +1] == '"')
                    {
                        // escaped quote
                        sb.Append('"');
                        i++; // skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            // add final field
            result.Add(sb.ToString());
            return result.ToArray();
        }

        /// <summary>
        /// Read-only view of all alarms.
        /// </summary>
        public static IReadOnlyDictionary<string, AlarmInfo> Alarms => _alarms;

        /// <summary>
        /// Try to get an alarm by code.
        /// </summary>
        public static bool TryGetAlarm(string code, out AlarmInfo? info)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                info = null;
                return false;
            }

            code = code.Trim();

            // first try
            if (_alarms.TryGetValue(code, out info))
                return true;

            // attempt to reload from file in case AlarmMessage.csv was changed after startup
            try
            {
                LoadFromFile();
            }
            catch
            {
                // ignore
            }

            // retry
            return _alarms.TryGetValue(code, out info);
        }

        /// <summary>
        /// Get alarm by code or null if not found.
        /// </summary>
        public static AlarmInfo? GetAlarm(string code)
        {
            return TryGetAlarm(code, out var info) ? info : null;
        }

        /// <summary>
        /// Search alarms by module or text fragment (case-insensitive).
        /// </summary>
        public static IEnumerable<AlarmInfo> Search(string? query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return _alarms.Values;

            query = query.Trim();
            return _alarms.Values.Where(a => a.Code.Contains(query, StringComparison.OrdinalIgnoreCase)
                                            || a.Module.Contains(query, StringComparison.OrdinalIgnoreCase)
                                            || a.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                                            || a.Solution.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Update the Solution text for an existing alarm code and persist the AlarmMessage.csv file.
        /// Returns true on success.
        /// </summary>
        public static bool UpdateSolution(string code, string solution)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim();

            lock (_fileLock)
            {
                try
                {
                    if (_alarms.TryGetValue(code, out var existing))
                    {
                        var updated = existing with { Solution = solution ?? string.Empty };
                        _alarms[code] = updated;
                    }
                    else
                    {
                        // If not existing, add a minimal entry
                        var added = new AlarmInfo(code, AlarmType.Warning, string.Empty, string.Empty, solution ?? string.Empty);
                        _alarms[code] = added;
                    }

                    // write full file
                    var baseDir = AppContext.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
                    var file = Path.Combine(baseDir, "AlarmMessage.csv");

                    var sb = new StringBuilder();
                    sb.AppendLine("Code,Type,Module,Description,Solution");
                    foreach (var kv in _alarms.Values)
                    {
                        sb.AppendLine(string.Join(",", EscapeCsv(kv.Code), EscapeCsv(kv.Type.ToString()), EscapeCsv(kv.Module), EscapeCsv(kv.Description), EscapeCsv(kv.Solution)));
                    }

                    File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
                    return true;
                }
                catch(Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
