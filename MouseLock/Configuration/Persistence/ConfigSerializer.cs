using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MouseLock.Configuration.Persistence;

internal static class ConfigSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string SerializeConfig(SystemConfiguration config)
    {
        config.EnsureInitialized();

        var json = JsonSerializer.Serialize(config, Options);
        return Convert.ToBase64String(Dalamud.Utility.Util.CompressString(json));
    }

    public static bool TryDeserializeConfig(
        string input,
        out SystemConfiguration? config,
        out string error)
    {
        config = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Clipboard is empty.";
            return false;
        }

        var trimmed = input.Trim();
        if (TryDecompress(trimmed, out var json) && TryDeserializeJson(json, out config, out error))
        {
            return true;
        }

        if (trimmed.StartsWith('{') && TryDeserializeJson(trimmed, out config, out error))
        {
            return true;
        }

        if (string.IsNullOrEmpty(error))
        {
            error = "Clipboard data was not valid MouseLock config data.";
        }

        return false;
    }

    private static bool TryDecompress(string input, out string json)
    {
        try
        {
            json = Dalamud.Utility.Util.DecompressString(Convert.FromBase64String(input));
            return true;
        }
        catch
        {
            json = string.Empty;
            return false;
        }
    }

    private static bool TryDeserializeJson(
        string json,
        out SystemConfiguration? config,
        out string error)
    {
        config = null;
        error = string.Empty;

        try
        {
            var imported = JsonSerializer.Deserialize<SystemConfiguration>(json, Options);
            if (imported is null)
            {
                error = "Clipboard data did not contain a MouseLock config.";
                return false;
            }

            if (imported.Version > SystemConfiguration.CurrentVersion)
            {
                error = $"Config version {imported.Version} is newer than supported version {SystemConfiguration.CurrentVersion}.";
                return false;
            }

            imported.EnsureInitialized();
            config = imported;
            return true;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            error = "Clipboard data could not be parsed as MouseLock config JSON.";
            return false;
        }
    }
}
