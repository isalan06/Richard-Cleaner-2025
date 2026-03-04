using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return _alarms.TryGetValue(code.Trim(), out info);
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
    }
}
