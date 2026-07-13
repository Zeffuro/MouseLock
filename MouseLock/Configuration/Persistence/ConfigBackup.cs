using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dalamud.Plugin;

namespace MouseLock.Configuration.Persistence;

public static class ConfigBackup
{
    private const int MaxBackups = 10;
    private const string Name = "MouseLock";

    public static void DoConfigBackup(IDalamudPluginInterface pluginInterface)
    {
        try
        {
            var configFiles = GetConfigFiles(pluginInterface).ToList();
            if (configFiles.Count == 0)
            {
                return;
            }

            var directoryInfo = GetBackupDirectory(pluginInterface);
            if (directoryInfo is null)
            {
                return;
            }

            if (!directoryInfo.Exists)
            {
                directoryInfo.Create();
            }

            var latestFile = new FileInfo(Path.Join(directoryInfo.FullName, $"{Name}.latest.zip"));
            var tempFile = Path.Join(directoryInfo.FullName, $"{Name}.tmp.zip");

            var needsBackup = !latestFile.Exists ||
                              ZipJsonHash(latestFile.FullName) != ConfigFilesJsonHash(configFiles, pluginInterface);
            if (!needsBackup)
            {
                return;
            }

            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            WriteConfigArchive(configFiles, tempFile, pluginInterface);
            if (latestFile.Exists)
            {
                var timestamp = latestFile.LastWriteTime;
                var archiveName = $"{Name}.{timestamp:yyyyMMddHHmmss}.zip";
                File.Move(latestFile.FullName, Path.Join(directoryInfo.FullName, archiveName));
            }

            File.Move(tempFile, latestFile.FullName);

            var oldBackups = directoryInfo.GetFiles($"{Name}.2*.zip")
                .OrderBy(file => file.LastWriteTimeUtc)
                .ToList();

            while (oldBackups.Count > MaxBackups)
            {
                oldBackups[0].Delete();
                oldBackups.RemoveAt(0);
            }
        }
        catch (Exception exception)
        {
            Service.Logger.Warning(exception, "Configuration backup skipped.");
        }
    }

    public static DirectoryInfo? GetBackupDirectory(IDalamudPluginInterface pluginInterface)
    {
        var launcherDirectory = pluginInterface.ConfigFile.Directory?.Parent ??
                                pluginInterface.ConfigDirectory.Parent?.Parent;
        return launcherDirectory is null
            ? null
            : new DirectoryInfo(Path.Join(launcherDirectory.FullName, "backups", Name));
    }

    public static bool TryLoadLatestBackup(out string configJson, out string message)
    {
        configJson = string.Empty;
        var directory = GetBackupDirectory(Service.PluginInterface);
        if (directory is null || !directory.Exists)
        {
            message = "No backup folder was found.";
            return false;
        }

        var backupFile = directory.GetFiles($"{Name}*.zip")
            .OrderByDescending(file => file.Name.Equals($"{Name}.latest.zip", StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();

        if (backupFile is null)
        {
            message = "No MouseLock backup was found.";
            return false;
        }

        using var zip = ZipFile.OpenRead(backupFile.FullName);
        var configEntryName = Service.PluginInterface.ConfigFile.Name;
        var entry = zip.Entries.FirstOrDefault(entry =>
            entry.FullName.Equals(configEntryName, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            message = $"Backup did not contain {configEntryName}.";
            return false;
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        configJson = reader.ReadToEnd();
        message = $"Loaded backup {backupFile.Name}.";
        return true;
    }

    private static IEnumerable<FileInfo> GetConfigFiles(IDalamudPluginInterface pluginInterface)
    {
        if (pluginInterface.ConfigFile.Exists)
        {
            yield return pluginInterface.ConfigFile;
        }

        if (!pluginInterface.ConfigDirectory.Exists)
        {
            yield break;
        }

        foreach (var file in pluginInterface.ConfigDirectory.GetFiles("*.json", SearchOption.TopDirectoryOnly)
                     .Where(IsConfigJsonFile))
        {
            if (!file.FullName.Equals(pluginInterface.ConfigFile.FullName, StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    private static void WriteConfigArchive(
        IEnumerable<FileInfo> configFiles,
        string archivePath,
        IDalamudPluginInterface pluginInterface)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
        foreach (var file in configFiles)
        {
            var entryName = GetArchiveEntryName(file, pluginInterface);
            archive.CreateEntryFromFile(file.FullName, entryName);
        }
    }

    private static string ConfigFilesJsonHash(IEnumerable<FileInfo> files, IDalamudPluginInterface pluginInterface)
        => ComputeCombinedJsonHash(files
            .Where(IsConfigJsonFile)
            .Select(file => (GetArchiveEntryName(file, pluginInterface), File.ReadAllBytes(file.FullName))));

    private static string ZipJsonHash(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var files = zip.Entries
            .Where(entry => entry.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            .Where(entry => !entry.FullName.EndsWith(".addon.json", StringComparison.OrdinalIgnoreCase))
            .Select(entry =>
            {
                using var stream = entry.Open();
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return (entry.FullName, memoryStream.ToArray());
            });

        return ComputeCombinedJsonHash(files);
    }

    private static string ComputeCombinedJsonHash(IEnumerable<(string Name, byte[] Contents)> files)
    {
        using var sha256 = SHA256.Create();
        foreach (var file in files.OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase))
        {
            var nameBytes = Encoding.UTF8.GetBytes(file.Name);
            sha256.TransformBlock(nameBytes, 0, nameBytes.Length, null, 0);
            sha256.TransformBlock(file.Contents, 0, file.Contents.Length, null, 0);
        }

        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        return BitConverter.ToString(sha256.Hash ?? []).Replace("-", string.Empty);
    }

    private static string GetArchiveEntryName(FileInfo file, IDalamudPluginInterface pluginInterface)
    {
        if (file.FullName.Equals(pluginInterface.ConfigFile.FullName, StringComparison.OrdinalIgnoreCase))
        {
            return file.Name;
        }

        return Path.Join(pluginInterface.InternalName, file.Name).Replace('\\', '/');
    }

    private static bool IsConfigJsonFile(FileInfo file)
        => file.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
           && !file.Name.EndsWith(".addon.json", StringComparison.OrdinalIgnoreCase);
}
