using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanerControlApp.Utilities
{
    public class AppSettings
    {
        public string Title { get; set; } = "DefaultValue";
        public bool EnableConsole { get; set; } = false;
        public bool EnableConsoleWindow { get; set; } = false;
    }
}
