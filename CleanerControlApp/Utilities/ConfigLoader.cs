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

        // Event raised when ModuleSettings are saved/updated so UI can refresh
        public static event Action<ModuleSettings>? ModuleSettingsUpdated;

        public static void Load()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

            // Ensure the Recipes folder exists and create a default recipe if there are no recipe files
            try
            {
                EnsureRecipesFolder();
                var hasAny = Directory.EnumerateFiles(RecipesFolder, "*.json").Any();
                if (!hasAny)
                {
                    // Export current ModuleSettings (from appsettings.json) into a default recipe
                    var moduleSettings = GetModuleSettings();
                    if (moduleSettings != null)
                    {
                        var defaultName = string.IsNullOrWhiteSpace(moduleSettings.RecipeName) ? "Default" : moduleSettings.RecipeName!;
                        // Use convenience method to save and set metadata
                        SaveRecipeForModuleSettings(moduleSettings, defaultName);
                    }
                }
            }
            catch
            {
                // Ignore errors here to avoid breaking configuration load; recipe persistence can be retried later
            }
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

        public static UnitSettings GetUnitSettings()
        { 
            var unitSettings = new UnitSettings();
            _configuration?.GetSection("UnitSettings")?.Bind(unitSettings);
            return unitSettings;
        }

        public static ModuleSettings GetModuleSettings()
        { 
            var moduleSettings = new ModuleSettings();
            _configuration?.GetSection($"{nameof(ModuleSettings)}")?.Bind(moduleSettings);

            try
            {
                // If no recipe specified, try to load Default and persist the RecipeName
                if (string.IsNullOrWhiteSpace(moduleSettings?.RecipeName))
                {
                    var defaultRecipe = LoadRecipe("Default");
                    if (defaultRecipe != null)
                    {
                        moduleSettings.ApplyRecipe(defaultRecipe);
                        moduleSettings.RecipeName = "Default";
                        // Persist the selected recipe name back into appsettings.json
                        try { SetModuleSettings(moduleSettings); } catch { }
                    }
                }
                else
                {
                    // Try loading the named recipe; if missing, fallback to Default and persist
                    var recipe = LoadRecipe(moduleSettings.RecipeName!);
                    if (recipe != null)
                    {
                        moduleSettings.ApplyRecipe(recipe);
                    }
                    else
                    {
                        var defaultRecipe = LoadRecipe("Default");
                        if (defaultRecipe != null)
                        {
                            moduleSettings.ApplyRecipe(defaultRecipe);
                            moduleSettings.RecipeName = "Default";
                            try { SetModuleSettings(moduleSettings); } catch { }
                        }
                    }
                }
            }
            catch
            {
                // Swallow errors to avoid breaking configuration reads; calling code can decide next steps
            }

            return moduleSettings;
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

        public static void SetUnitSettings(UnitSettings unitSettings)
        {
            if (unitSettings == null) throw new ArgumentNullException(nameof(unitSettings));

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
            var commNode = JsonSerializer.SerializeToNode(unitSettings, options: new JsonSerializerOptions { WriteIndented = false });
            if (commNode == null) commNode = JsonObject.Parse("{}");

            root["UnitSettings"] = commNode;

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

        public static void SetModuleSettings(ModuleSettings moduleSettings)
        {
            if (moduleSettings == null) throw new ArgumentNullException(nameof(moduleSettings));

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
            var commNode = JsonSerializer.SerializeToNode(moduleSettings, options: new JsonSerializerOptions { WriteIndented = false });
            if (commNode == null) commNode = JsonObject.Parse("{}");

            root["ModuleSettings"] = commNode;

            // Write back
            var options = new JsonSerializerOptions { WriteIndented = true };
            var newJson = root.ToJsonString(options);
            File.WriteAllText(path, newJson, Encoding.UTF8);

            // Reload configuration
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            _configuration = builder.Build();

            // If ModuleSettings specifies a RecipeName, also save the linked recipe file to keep it in sync
            try
            {
                if (!string.IsNullOrWhiteSpace(moduleSettings.RecipeName))
                {
                    SaveRecipeForModuleSettings(moduleSettings, moduleSettings.RecipeName!);
                }
            }
            catch
            {
                // Ignore recipe persistence errors to avoid breaking config save
            }
        }

        // --- Recipe file helpers ---
        private static string RecipesFolder => Path.Combine(AppContext.BaseDirectory, "Recipes");

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Recipe name is empty.", nameof(name));
            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(name.Length);
            foreach (var ch in name)
            {
                sb.Append(invalid.Contains(ch) ? '_' : ch);
            }
            return sb.ToString();
        }

        private static string GetRecipeFilePath(string name)
        {
            var safe = SanitizeFileName(name);
            return Path.Combine(RecipesFolder, safe + ".json");
        }

        public static void EnsureRecipesFolder()
        {
            if (!Directory.Exists(RecipesFolder)) Directory.CreateDirectory(RecipesFolder);
        }

        public static void SaveRecipe(Recipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (string.IsNullOrWhiteSpace(recipe.Name)) throw new ArgumentException("Recipe must have a Name.", nameof(recipe));

            EnsureRecipesFolder();
            var path = GetRecipeFilePath(recipe.Name!);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(recipe, options);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        public static Recipe? LoadRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Recipe name is empty.", nameof(name));
            var path = GetRecipeFilePath(name);
            if (!File.Exists(path)) return null;

            var json = File.ReadAllText(path, Encoding.UTF8);
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var recipe = JsonSerializer.Deserialize<Recipe>(json, options);
                return recipe;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        public static bool DeleteRecipe(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            var path = GetRecipeFilePath(name);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }

        public static List<string> ListRecipeNames()
        {
            EnsureRecipesFolder();
            var list = new List<string>();
            foreach (var file in Directory.EnumerateFiles(RecipesFolder, "*.json"))
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    list.Add(name);
                }
                catch { }
            }
            return list;
        }

        // Convenience: export current ModuleSettings to a recipe file
        public static void SaveRecipeForModuleSettings(ModuleSettings moduleSettings, string recipeName)
        {
            if (moduleSettings == null) throw new ArgumentNullException(nameof(moduleSettings));
            if (string.IsNullOrWhiteSpace(recipeName)) throw new ArgumentException("Recipe name required.", nameof(recipeName));

            var recipe = moduleSettings.ExportRecipe(recipeName);
            SaveRecipe(recipe);

            // Optionally update ModuleSettings metadata
            moduleSettings.RecipeName = recipeName;
        }

        // Convenience: load a recipe file and apply to ModuleSettings (and set RecipeName)
        public static bool LoadRecipeToModuleSettings(ModuleSettings moduleSettings, string recipeName)
        {
            if (moduleSettings == null) throw new ArgumentNullException(nameof(moduleSettings));
            if (string.IsNullOrWhiteSpace(recipeName)) throw new ArgumentException("Recipe name required.", nameof(recipeName));

            var recipe = LoadRecipe(recipeName);
            if (recipe == null) return false;

            moduleSettings.ApplyRecipe(recipe);
            moduleSettings.RecipeName = recipeName;
            return true;
        }

        public static void SetModuleSettingsAndSave(ModuleSettings moduleSettings)
        {
            // Persist ModuleSettings to appsettings.json
            SetModuleSettings(moduleSettings);

            // If a recipe name is selected, also save that recipe file so external recipe files stay in sync
            try
            {
                if (!string.IsNullOrWhiteSpace(moduleSettings?.RecipeName))
                {
                    SaveRecipeForModuleSettings(moduleSettings, moduleSettings.RecipeName!);
                }
            }
            catch
            {
                // ignore recipe save failures
            }

            // Notify subscribers that ModuleSettings have been updated
            try
            {
                ModuleSettingsUpdated?.Invoke(moduleSettings);
            }
            catch
            {
                // ignore subscriber exceptions
            }
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
