using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NgenuityBattertyTrayApp.Settings;

internal sealed class AppSettings
{
    public ulong? SelectedBaseId { get; set; }
    public bool StartWithWindows { get; set; }
    public int PollIntervalSeconds { get; set; } = 30;

    [JsonIgnore]
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static AppSettings LoadOrDefault(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, JsonOptions);
        File.WriteAllText(path, json);
    }
}


