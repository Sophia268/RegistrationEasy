using System.IO;
using System.Text;
using System.Text.Json;

namespace AutoDeployTool.Services;

public class AppConfig
{
    public string Password { get; set; } = string.Empty;
    public string URI { get; set; } = string.Empty;
}

public static class ConfigProvider
{
    public static AppConfig Get()
    {
        var baseDir = AppContext.BaseDirectory;
        var path = Path.Combine(baseDir, "config.json");
        if (!File.Exists(path))
        {
            var alt = Path.Combine(baseDir, "RegistrationEasy", "config.json");
            if (File.Exists(alt)) path = alt;
        }

        var json = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8) : "{}";
        var cfg = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new AppConfig();
        return cfg;
    }
}
