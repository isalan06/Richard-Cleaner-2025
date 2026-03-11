using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CleanerControlApp.Utilities.Log;
using CleanerControlApp.Modules.UserManagement.Services;

namespace CleanerControlApp.Utilities.Alarm
{
    /// <summary>
    /// Represents a single alarm entry in the manager's list.
    /// </summary>
    public sealed record AlarmEntry
    {
        public string Code { get; init; }
        public AlarmType Type { get; init; }
        public string Module { get; init; }
        public string Description { get; init; }
        public bool IsAlarm { get; internal set; }
        public DateTime? HappenTime { get; internal set; }
        public string? AlarmSN { get; internal set; }

        public AlarmEntry(string code, AlarmType type, string module, string description)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Type = type;
            Module = module ?? string.Empty;
            Description = description ?? string.Empty;
            IsAlarm = false;
            HappenTime = null;
            AlarmSN = null;
        }
    }

    /// <summary>
    /// Manages alarms as a static list loaded from AlarmList and writes logs when statuses change.
    /// </summary>
    public static class AlarmManager
    {
        private static readonly object _lock = new();
        // All alarms known by the system (loaded from AlarmList)
        private static readonly Dictionary<string, AlarmEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

        // external flag getters keyed by composite key (code[:instanceId])
        private static readonly Dictionary<string, Func<bool>> _flagGetters = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, bool> _previousFlagValues = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Base directory for alarm logs. Defaults to application's base directory + "AlarmLog".
        /// </summary>
        public static string LogBaseDirectory { get; set; } = Path.Combine(AppContext.BaseDirectory ?? AppDomain.CurrentDomain.BaseDirectory, "AlarmLog");

        /// <summary>
        /// Event raised when alarm entries change (added/updated/removed). UI can subscribe to refresh bindings.
        /// </summary>
        public static event Action? AlarmsChanged;

        private static void RaiseAlarmsChanged()
        {
            try
            {
                AlarmsChanged?.Invoke();
            }
            catch
            {
                // swallow exceptions from subscribers
            }
        }

        private static string MakeCompositeKey(string code, string? instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
                return code;
            return code + ":" + instanceId;
        }

        private static string ExtractCodeFromComposite(string compositeKey)
        {
            var idx = compositeKey.IndexOf(':');
            if (idx < 0) return compositeKey;
            return compositeKey.Substring(0, idx);
        }

        private static string GetLogUserName()
        {
            return UserManager.CurrentUsername ?? "System";
        }

        /// <summary>
        /// Load alarms from AlarmList into the internal static list. Optionally pass currently active codes that should be marked as active at startup.
        /// </summary>
        public static void Initialize(IEnumerable<string>? activeAlarmCodes = null)
        {
            lock (_lock)
            {
                _entries.Clear();
                foreach (var kv in AlarmList.Alarms)
                {
                    var info = kv.Value;
                    var entry = new AlarmEntry(info.Code, info.Type, info.Module, info.Description);
                    _entries[entry.Code] = entry;
                }

                if (activeAlarmCodes != null)
                {
                    var now = DateTime.Now; // use local time
                    foreach (var code in activeAlarmCodes)
                    {
                        if (string.IsNullOrWhiteSpace(code))
                            continue;

                        if (_entries.TryGetValue(code, out var e))
                        {
                            if (!e.IsAlarm)
                            {
                                e.IsAlarm = true;
                                e.HappenTime = now;
                                e.AlarmSN = GenerateAlarmSN(code, now);
                                WriteLog(e, "Alarm", now);
                                // Also write an operation log for the alarm event
                                OperateLog.Log($"{e.Module}發生錯誤", GetLogUserName(), $"{e.Code}-{e.Description}");
                            }
                        }
                        else
                        {
                            // Unknown code - try to get info from AlarmList then create minimal entry and log
                            if (AlarmList.TryGetAlarm(code, out var info))
                            {
                                var entry = new AlarmEntry(info.Code, info.Type, info.Module, info.Description)
                                {
                                    IsAlarm = true,
                                    HappenTime = now,
                                    AlarmSN = GenerateAlarmSN(code, now)
                                };
                                _entries[code] = entry;
                                WriteLog(entry, "Alarm", now);
                                OperateLog.Log($"{entry.Module}發生錯誤", GetLogUserName(), $"{entry.Code}-{entry.Description}");
                            }
                            else
                            {
                                var entry = new AlarmEntry(code, AlarmType.Warning, string.Empty, string.Empty)
                                {
                                    IsAlarm = true,
                                    HappenTime = now,
                                    AlarmSN = GenerateAlarmSN(code, now)
                                };
                                _entries[code] = entry;
                                WriteLog(entry, "Alarm", now);
                                OperateLog.Log($"{entry.Module}發生錯誤", GetLogUserName(), $"{entry.Code}-{entry.Description}");
                            }
                        }
                    }
                }
            }

            // notify listeners that initial set changed
            RaiseAlarmsChanged();
        }

        /// <summary>
        /// Update the alarm flag for a specific code. If the flag differs from the stored state, write a log and update the internal table.
        /// </summary>
        public static void UpdateFlag(string code, bool isAlarm, DateTime? timestamp = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            timestamp ??= DateTime.Now; // use local time

            bool changed = false;

            lock (_lock)
            {
                if (!_entries.TryGetValue(code, out var entry))
                {
                    // Try to populate from AlarmList first
                    if (AlarmList.TryGetAlarm(code, out var info))
                    {
                        if (info != null)
                            entry = new AlarmEntry(info.Code, info.Type, info.Module, info.Description);
                    }
                    else
                    {
                        // Create minimal entry for unknown code
                        entry = new AlarmEntry(code, AlarmType.Warning, string.Empty, string.Empty);
                    }
                    _entries[code] = (entry != null) ? entry: new AlarmEntry(code, AlarmType.Warning, string.Empty, string.Empty);                }
                else
                {
                    // If we have a stored entry but module/description are empty, try to fill from AlarmList
                    if ((string.IsNullOrEmpty(entry.Module) || string.IsNullOrEmpty(entry.Description)) && AlarmList.TryGetAlarm(code, out var info2))
                    {
                        // Replace stored entry with one that has Module/Description and Type
                        entry = new AlarmEntry(info2.Code, info2.Type, info2.Module, info2.Description)
                        {
                            IsAlarm = entry.IsAlarm,
                            HappenTime = entry.HappenTime,
                            AlarmSN = entry.AlarmSN
                        };
                        _entries[code] = entry;
                    }
                }

                if (entry.IsAlarm == isAlarm)
                    return; // no change

                if (isAlarm)
                {
                    // Alarm started
                    entry.IsAlarm = true;
                    entry.HappenTime = timestamp;
                    entry.AlarmSN = GenerateAlarmSN(code, timestamp.Value);
                    WriteLog(entry, "Alarm", timestamp.Value);
                    // Also write an operation log for the alarm event
                    OperateLog.Log($"{entry.Module}發生錯誤", GetLogUserName(), $"{entry.Code}-{entry.Description}");
                    changed = true;
                }
                else
                {
                    // Alarm finished
                    var sn = entry.AlarmSN ?? GenerateAlarmSN(code, timestamp.Value);
                    // Log finish using the existing SN (or generated if missing)
                    WriteLog(entry with { AlarmSN = sn }, "AlarmFinish", timestamp.Value);

                    // Update stored state
                    entry.IsAlarm = false;
                    entry.HappenTime = null;
                    entry.AlarmSN = null;
                    changed = true;
                }
            }

            if (changed)
            {
                RaiseAlarmsChanged();
            }
        }

        /// <summary>
        /// Attach an external flag getter for a code. Call CheckFlagGetters periodically to detect changes.
        /// Optional instanceId allows multiple registrations for same code.
        /// </summary>
        public static void AttachFlagGetter(string code, Func<bool> getter, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));
            if (getter == null)
                throw new ArgumentNullException(nameof(getter));

            var key = MakeCompositeKey(code, instanceId);

            lock (_lock)
            {
                _flagGetters[key] = getter;
                _previousFlagValues[key] = getter();

                // Ensure table reflects current state (use base code)
                UpdateFlag(code, _previousFlagValues[key]);
            }
        }

        /// <summary>
        /// Detach a previously attached flag getter. Use same instanceId used when attaching if provided.
        /// </summary>
        public static void DetachFlagGetter(string code, string? instanceId = null)
        {
            if (string.IsNullOrWhiteSpace(code))
                return;

            var key = MakeCompositeKey(code, instanceId);

            lock (_lock)
            {
                _flagGetters.Remove(key);
                _previousFlagValues.Remove(key);
            }
        }

        /// <summary>
        /// Check all attached flag getters for changes and update table/logs accordingly.
        /// </summary>
        public static void CheckFlagGetters()
        {
            KeyValuePair<string, Func<bool>>[] snapshot;
            lock (_lock)
            {
                snapshot = _flagGetters.ToArray();
            }

            foreach (var kv in snapshot)
            {
                var compositeKey = kv.Key;
                bool current;
                try
                {
                    current = kv.Value();
                }
                catch
                {
                    continue; // ignore exceptions from getters
                }

                bool previous;
                lock (_lock)
                {
                    previous = _previousFlagValues.TryGetValue(compositeKey, out var v) ? v : false;
                }

                if (current != previous)
                {
                    lock (_lock)
                    {
                        _previousFlagValues[compositeKey] = current;
                    }

                    // Extract original code and update
                    var code = ExtractCodeFromComposite(compositeKey);
                    UpdateFlag(code, current);
                }
            }
        }

        /// <summary>
        /// Returns a snapshot of the alarm entries.
        /// </summary>
        public static IReadOnlyList<AlarmEntry> GetAllEntries()
        {
            lock (_lock)
            {
                return _entries.Values.ToList();
            }
        }

        /// <summary>
        /// Try get a single alarm entry by code.
        /// </summary>
        public static AlarmEntry? GetEntry(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            lock (_lock)
            {
                return _entries.TryGetValue(code, out var e) ? e : null;
            }
        }

        private static string GenerateAlarmSN(string code, DateTime time)
        {
            // Code + local timestamp (yyyyMMddHHmmssfff)
            return $"{code}_{time:yyyyMMddHHmmssfff}";
        }

        private static void WriteLog(AlarmEntry entry, string status, DateTime time)
        {
            try
            {
                var baseDir = LogBaseDirectory;
                var ym = time.ToString("yyyyMM"); // time is local
                var day = time.ToString("yyyyMMdd");
                var folder = Path.Combine(baseDir, ym);
                Directory.CreateDirectory(folder);

                var file = Path.Combine(folder, day + ".csv");

                // If new file, write header
                if (!File.Exists(file))
                {
                    var header = "AlarmCode,Type,Status,Time,Module,Description,AlarmSN" + Environment.NewLine;
                    File.AppendAllText(file, header, Encoding.UTF8);
                }

                // Determine the code to write. If AlarmSN contains a code prefix, prefer that
                string codeToWrite = entry.Code ?? string.Empty;
                if (!string.IsNullOrEmpty(entry.AlarmSN))
                {
                    var uidx = entry.AlarmSN.IndexOf('_');
                    if (uidx > 0)
                    {
                        var prefix = entry.AlarmSN.Substring(0, uidx);
                        if (!string.IsNullOrEmpty(prefix))
                            codeToWrite = prefix;
                    }
                }

                // If possible, prefer Module/Description from AlarmList using the final codeToWrite
                string module = entry.Module ?? string.Empty;
                string description = entry.Description ?? string.Empty;
                var type = entry is null ? AlarmType.Warning : entry.Type;
                if (AlarmList.TryGetAlarm(codeToWrite, out var info))
                {
                    if (!string.IsNullOrEmpty(info.Module))
                        module = info.Module;
                    if (!string.IsNullOrEmpty(info.Description))
                        description = info.Description;
                    type = info.Type;
                }

                // Prepare CSV line with proper escaping
                string Escape(string? s)
                {
                    if (string.IsNullOrEmpty(s))
                        return string.Empty;
                    if (s.Contains('"'))
                        s = s.Replace("\"", "\"\"");
                    if (s.IndexOfAny(new char[] { ',', '"', '\r', '\n' }) >= 0)
                        return '"' + s + '"';
                    return s;
                }

                var line = string.Join(',', new string[]
                {
                     Escape(codeToWrite),
                     Escape(type.ToString()),
                     Escape(status),
                     Escape(time.ToString("yyyy-MM-dd HH:mm:ss")), // local time formatted
                     Escape(module),
                     Escape(description),
                     Escape(entry.AlarmSN)
                });

                File.AppendAllText(file, line + Environment.NewLine, Encoding.UTF8);

                // removed duplicate OperateLog call here to avoid double entries
            }
            catch
            {
                // Swallow logging exceptions to avoid crashing alarm checks
            }
        }

        /// <summary>
        /// Clears all entries and registered getters. Use with care.
        /// </summary>
        public static void ClearAll()
        {
            lock (_lock)
            {
                _entries.Clear();
                _flagGetters.Clear();
                _previousFlagValues.Clear();
            }

            RaiseAlarmsChanged();
        }
    }
}
