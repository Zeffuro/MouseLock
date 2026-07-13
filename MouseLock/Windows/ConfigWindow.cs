using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.Windows.Components;
using MouseLock.Windows.Tabs;

namespace MouseLock.Windows;

internal sealed partial class ConfigWindow : Window
{
    private readonly SystemConfiguration _config;
    private readonly MouseLookStatusCard _statusCard = new();
    private readonly ConfigurationTransferPanel _configurationTransferPanel;
    private readonly HotbarSlotPicker _hotbarSlotPicker;
    private readonly ToggleKeybindEditor _toggleKeybindEditor;
    private readonly NativeAddonExceptionEditor _nativeAddonExceptionEditor;
    private readonly DiagnosticsPanel _diagnosticsPanel;
    private readonly DtrSettingsEditor _dtrSettingsEditor;
    private readonly GeneralTab _generalTab;
    private readonly ActivationTab _activationTab;
    private readonly MouseActionsTab _mouseActionsTab;
    private readonly CompatibilityTab _compatibilityTab;
    private readonly DiagnosticsTab _diagnosticsTab;

    public ConfigWindow(SystemConfiguration config) : base("MouseLock Config")
    {
        _config = config;
        _configurationTransferPanel = new ConfigurationTransferPanel(_config);
        _hotbarSlotPicker = new HotbarSlotPicker(Save);
        _toggleKeybindEditor = new ToggleKeybindEditor(Save);
        _nativeAddonExceptionEditor = new NativeAddonExceptionEditor(Save);
        _diagnosticsPanel = new DiagnosticsPanel(_config, Save);
        _dtrSettingsEditor = new DtrSettingsEditor(Save);
        _generalTab = new GeneralTab(
            _config,
            Save,
            _toggleKeybindEditor,
            _dtrSettingsEditor,
        _configurationTransferPanel);
        _activationTab = new ActivationTab(_config, Save, _nativeAddonExceptionEditor);
        _mouseActionsTab = new MouseActionsTab(_config, Save, _hotbarSlotPicker);
        _compatibilityTab = new CompatibilityTab(_config, Save);
        _diagnosticsTab = new DiagnosticsTab(_diagnosticsPanel);
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = ImGuiHelpers.ScaledVector2(600.0f, 550.0f),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };
    }

    public override void Draw()
    {
        _statusCard.Draw();

        using var tabBar = ImRaii.TabBar("MouseLockConfigTabs");
        if (!tabBar)
        {
            return;
        }

        _generalTab.Draw();
        _activationTab.Draw();
        _mouseActionsTab.Draw();
        _compatibilityTab.Draw();
        _diagnosticsTab.Draw();
    }

    private void Save()
    {
        ConfigRepository.Save(_config);
        PluginState.MouseLookService?.RefreshCurrentStatus();
        PluginState.DtrStatusService?.Refresh();
    }

}
