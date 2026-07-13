using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dalamud.Game.Command;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Commands;

public sealed class CommandHandler : IDisposable
{
    private const string MainCommand = "/MouseLock";
    private const string HelpDescription = "MouseLock command. Use '/MouseLock help' for options.";

    private readonly Dictionary<string, SubCommand> subCommands;

    public CommandHandler()
    {
        this.subCommands = new Dictionary<string, SubCommand>(StringComparer.OrdinalIgnoreCase)
        {
            ["help"] = new(_ => this.PrintHelp(), "Show this help message"),
            ["toggle"] = new(_ => ToggleMain(), "Toggle the main window"),
            ["open"] = new(_ => OpenMain(), "Open the main window"),
            ["close"] = new(_ => CloseMain(), "Close the main window"),
            ["config"] = new(_ => ToggleConfig(), "Toggle the configuration window"),
            ["settings"] = new(_ => ToggleConfig(), "Toggle the configuration window"),
            ["save"] = new(_ => SaveConfig(), "Save configuration immediately"),
            ["reset"] = new(_ => ResetConfig(), "Reset configuration to defaults"),
        };

        Services.CommandManager.AddHandler(MainCommand, new CommandInfo(this.OnCommand)
        {
            DisplayOrder = 1,
            ShowInHelp = true,
            HelpMessage = HelpDescription,
        });
    }

    public void Dispose()
    {
        Services.CommandManager.RemoveHandler(MainCommand);
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

        if (this.subCommands.TryGetValue(subCommandName, out var subCommand))
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

    private static void ToggleMain()
    {
        System.MainWindow.Toggle();
    }

    private static void OpenMain()
    {
        System.MainWindow.IsOpen = true;
    }

    private static void CloseMain()
    {
        System.MainWindow.IsOpen = false;
    }

    private static void ToggleConfig()
    {
        System.ConfigWindow.Toggle();
    }

    private static void SaveConfig()
    {
        ConfigRepository.SaveImmediate(System.Config);
        PrintChat("Configuration saved.");
    }

    private static void ResetConfig()
    {
        System.Config = ConfigRepository.Reset();
        PrintChat("Configuration reset.");
    }

    private void PrintHelp()
    {
        var builder = new StringBuilder("MouseLock Commands:\n");
        foreach (var (name, subCommand) in this.subCommands.OrderBy(pair => pair.Key))
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
        Services.ChatGui.Print(message, "MouseLock");
    }

    private sealed record SubCommand(Action<string> Action, string Description, string Usage = "");
}