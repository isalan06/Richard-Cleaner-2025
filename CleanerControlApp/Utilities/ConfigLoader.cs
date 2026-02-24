using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanerControlApp.Utilities
{
    public class ConfigLoader
    {
        private static IConfiguration? _configuration;

        public static void Load()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static AppSettings GetSettings()
        {
            var settings = new AppSettings();
            _configuration?.GetSection("AppSettings")?.Bind(settings);
            return settings;
        }

        public static CommunicationSettings GetCommunicationSettings()
        {
            var communicationSettings = new CommunicationSettings();
            _configuration?.GetSection("CommunicationSettings")?.Bind(communicationSettings);
            return communicationSettings;
        }

        public static void SetSettings(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Configuration file not found.", path);

            // Read existing JSON
            var jsonText = File.ReadAllText(path);
            JsonObject? root = null;
            try
            {
                root = JsonNode.Parse(jsonText)?.AsObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
            }
            if (root == null) root = new JsonObject();

            // Replace AppSettings section
            var appSettingsNode = JsonSerializer.SerializeToNode(settings, options: new JsonSerializerOptions { WriteIndented = false });
            if (appSettingsNode == null) appSettingsNode = JsonObject.Parse("{}");

            root["AppSettings"] = appSettingsNode;

            // Write back
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = root.ToJsonString(options);
            File.WriteAllText(path, newJson, Encoding.UTF8);

            // Reload configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static void SetCommunicationSettings(CommunicationSettings communicationSettings)
        {
            if (communicationSettings == null) throw new ArgumentNullException(nameof(communicationSettings));

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            if (!File.Exists(path)) throw new FileNotFoundException("Configuration file not found.", path);

            // Read existing JSON
            var jsonText = File.ReadAllText(path);
            JsonObject? root = null;
            try
            {
                root = JsonNode.Parse(jsonText)?.AsObject();
            }
            catch (JsonException)
            {
                root = new JsonObject();
            }
            if (root == null) root = new JsonObject();

            // Replace CommunicationSettings section
            var commNode = JsonSerializer.SerializeToNode(communicationSettings, options: new JsonSerializerOptions { WriteIndented = false });
            if (commNode == null) commNode = JsonObject.Parse("{}");

            root["CommunicationSettings"] = commNode;

            // Write back
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = root.ToJsonString(options);
            File.WriteAllText(path, newJson, Encoding.UTF8);

            // Reload configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();
        }

        public static void Save()
        {
            if (_configuration == null) throw new InvalidOperationException("Configuration not loaded.");

            var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            // Build JSON from IConfiguration
            var root = new JsonObject();
            foreach (var section in _configuration.GetChildren())
            {
                var node = BuildNode(section);
                if (node != null)
                    root[section.Key] = node;
                else
                    root[section.Key] = JsonValue.Create(section.Value);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, root.ToJsonString(options), Encoding.UTF8);
        }

        private static JsonNode? BuildNode(IConfigurationSection section)
        {
            var children = section.GetChildren().ToList();
            if (!children.Any())
            {
                return section.Value != null ? JsonValue.Create(section.Value) : JsonValue.Create((string?)null);
            }

            // Detect array-like children (keys are numeric)
            var isArray = children.All(c => int.TryParse(c.Key, out _));
            if (isArray)
            {
                var arr = new JsonArray();
                foreach (var child in children.OrderBy(c => int.Parse(c.Key)))
                {
                    var node = BuildNode(child);
                    if (node != null) arr.Add(node);
                    else arr.Add(JsonValue.Create(child.Value));
                }
                return arr;
            }

            var obj = new JsonObject();
            foreach (var child in children)
            {
                var node = BuildNode(child);
                if (node != null)
                    obj[child.Key] = node;
                else
                    obj[child.Key] = JsonValue.Create(child.Value);
            }
            return obj;
        }
    }
}
