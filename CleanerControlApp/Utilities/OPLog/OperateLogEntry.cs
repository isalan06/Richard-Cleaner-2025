using System;

namespace CleanerControlApp.Utilities.Log
{
 public class OperateLogEntry
 {
 public DateTime Timestamp { get; set; }
 public string EventName { get; set; } = string.Empty;
 public string UserName { get; set; } = string.Empty;
 public string Description { get; set; } = string.Empty;
 }
}
