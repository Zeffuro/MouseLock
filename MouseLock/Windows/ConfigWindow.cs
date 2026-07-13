using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;

namespace MouseLock.Windows;

public sealed class ConfigWindow : Window, IDisposable
{
    private readonly SystemConfiguration config;

    public ConfigWindow(SystemConfiguration config) : base("MouseLock Config")
    {
        this.config = config;
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(320.0f, 160.0f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        var enabled = this.config.General.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            this.config.General.Enabled = enabled;
            ConfigRepository.Save(this.config);
        }

        var debugEnabled = this.config.General.DebugEnabled;
        if (ImGui.Checkbox("Debug enabled", ref debugEnabled))
        {
            this.config.General.DebugEnabled = debugEnabled;
            ConfigRepository.Save(this.config);
        }
    }

    public void Dispose()
    {
    }
}