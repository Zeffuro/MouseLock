using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using MouseLock.Configuration;
using MouseLock.MouseLook;
using MouseLock.MouseLook.Activation;

namespace MouseLock.Commands;

internal sealed class CommandHandler : IDisposable
{
    private const string MainCommand = "/mouselock";
    private const string HelpDescription = "MouseLock command. Use '/mouselock help' for options.";

    private readonly Dictionary<string, SubCommand> _subCommands;

    public CommandHandler()
    {
        _subCommands = new Dictionary<string, SubCommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = new(_ => PrintHelp(), "Show this help message"),
            ["off"] = new(_ => SetEnabled(false), "Disable Mouselook"),
            ["on"] = new(_ => SetEnabled(true), "Enable Mouselook"),
            ["status"] = new(_ => PrintStatus(), "Show current status"),
            ["suspend"] = new(HandleSuspend, "Toggle or control command-based suspension", "[on|off|resume|clear|status] [source]"),
            ["toggle"] = new(_ => ToggleEnabled(), "Toggle Mouselook"),
        };

        Service.CommandManager.AddHandler(MainCommand, new CommandInfo(OnCommand)
        {
            DisplayOrder = 1,
            ShowInHelp = true,
            HelpMessage = HelpDescription,
        });
    }

    public void Dispose()
    {
        Service.CommandManager.RemoveHandler(MainCommand);
    }

    private void OnCommand(string command, string args)
    {
        var parts = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            PluginState.ConfigWindow.Toggle();
            return;
        }

        var subCommandName = parts[0];
        var subArgs = parts.Length > 1 ? parts[1] : string.Empty;

        if (_subCommands.TryGetValue(subCommandName, out var subCommand))
        {
            subCommand.Action(subArgs);
            return;
        }

        PrintChat($"Unknown command: {subCommandName}. Use '{MainCommand} help' for available commands.");
    }

    private static void SetEnabled(bool enabled)
    {
        MouseLockStateController.SetEnabled(enabled);
        PrintStatus();
    }

    private static void ToggleEnabled()
    {
        MouseLockStateController.ToggleEnabled();
        PrintStatus();
    }

    private static void PrintStatus()
    {
        var status = PluginState.MouseLookService?.Status
                     ?? MouseLookStatus.Unavailable(MouseLookPauseReason.HookUnavailable);
        PrintChat($"{MouseLookStatusFormatter.GetSummary(status)} - {MouseLookStatusFormatter.GetDetail(status)}");
    }

    private static void HandleSuspend(string args)
    {
        var parts = args.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            SetSuspended(DefaultCommandSuspensionSource, !SuspensionRegistry.IsSuspendedBy(DefaultCommandSuspensionSource));
            return;
        }

        var action = parts[0];
        var source = parts.Length > 1 ? parts[1] : DefaultCommandSuspensionSource;
        switch (action.ToLowerInvariant())
        {
            case "on":
                SetSuspended(source, true);
                break;

            case "off":
            case "resume":
                SetSuspended(source, false);
                break;

            case "clear":
                ClearSuspensions();
                break;

            case "status":
                PrintSuspensionStatus();
                break;

            default:
                PrintChat($"Unknown suspend action: {action}. Use '{MainCommand} suspend on|off|clear|status [source]'.");
                break;
        }
    }

    private const string DefaultCommandSuspensionSource = "Command";

    private static void SetSuspended(string source, bool suspended)
    {
        SuspensionRegistry.SetSuspended(source, suspended);
        PluginState.MouseLookService?.RefreshCurrentStatus();
        PrintChat(suspended
            ? $"Suspended by {source}."
            : $"Released suspension for {source}.");
        PrintSuspensionStatus();
    }

    private static void ClearSuspensions()
    {
        var clearedCount = SuspensionRegistry.Clear();
        PluginState.MouseLookService?.RefreshCurrentStatus();
        PrintChat($"Cleared {clearedCount} suspension source(s).");
        PrintSuspensionStatus();
    }

    private static void PrintSuspensionStatus()
    {
        PrintChat(SuspensionRegistry.IsSuspended
            ? $"Suspended by: {SuspensionRegistry.SourcesSummary}"
            : "No external suspensions are active.");
    }

    private void PrintHelp()
    {
        var builder = new StringBuilder("MouseLock Commands:\n");
        foreach (var (name, subCommand) in _subCommands.OrderBy(pair => pair.Key))
        {
            builder.Append(MainCommand).Append(' ').Append(name);
            if (!string.IsNullOrWhiteSpace(subCommand.Usage))
            {
                builder.Append(' ').Append(subCommand.Usage);
            }

            builder.Append(" - ").AppendLine(subCommand.Description);
        }

        PrintChat(builder.ToString());
    }

    private static void PrintChat(string message)
    {
        Service.ChatGui.Print(message, "MouseLock");
    }

    private sealed record SubCommand(Action<string> Action, string Description, string Usage = "");
}
