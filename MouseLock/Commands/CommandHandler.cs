using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using MouseLock.Configuration;

namespace MouseLock.Commands;

public sealed class CommandHandler : IDisposable
{
    private const string MainCommand = "/mouselock";
    private const string HelpDescription = "MouseLock command. Use '/mouselock help' for options.";

    private readonly Dictionary<string, SubCommand> _subCommands;

    public CommandHandler()
    {
        _subCommands = new Dictionary<string, SubCommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = new(_ => PrintHelp(), "Show this help message"),
            ["toggle"] = new(_ => MouseLockSettingsActions.ToggleEnabled(), "Toggle Mouselook"),
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
            HandleDefaultCommand();
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

    private static void HandleDefaultCommand()
    {
        ToggleConfig();
    }

    private static void ToggleConfig()
    {
        PluginState.ConfigWindow.Toggle();
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
