using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using MouseLock.Configuration;
using MouseLock.Targeting;

namespace MouseLock.Windows.Components;

internal sealed class ReticleSettingsEditor(SystemConfiguration config, Action save)
{
    private static readonly string[] ShapeLabels = ["Crosshair", "Icon", "Preset", "Arcade"];
    private static readonly string[] IconLabels = ["Red Diamond", "Gold Diamond", "Green Dot", "Red Dot", "Red Ring", "Gold Dot", "White Dot", "Gold Marker", "Custom"];
    private static readonly string[] DesignLabels = ["Spokes", "Halo", "Diamond", "Petals", "Star", "Arrows"];
    private static readonly string[] VariantLabels = ["Base", "Open", "Ring", "Alt"];
    private bool _customIcon;
    private bool _previewTarget;

    internal bool PreviewTarget => _previewTarget;

    internal void Draw()
    {
        var settings = config.Targeting;
        ConfigWindow.DrawSection("Reticle");
        var offset = settings.VerticalOffset;
        if (ImGui.SliderFloat("Vertical offset (% of screen height)", ref offset, -45, 45, "%.1f%%"))
        {
            settings.VerticalOffset = offset;
            save();
        }
        ConfigWindow.DrawTooltip("Positive moves up, negative moves down. Also applies when the reticle is hidden.");

        var show = settings.ShowReticle;
        if (ImGui.Checkbox("Show reticle during mouselook", ref show))
        {
            settings.ShowReticle = show;
            save();
        }
        ConfigWindow.DrawTooltip("Works with targeting turned off too. This tab shows a preview.");
        using var disabled = ImRaii.Disabled(!settings.ShowReticle);
        var shape = (int)settings.ReticleStyle - 1;
        if (ImGui.Combo("Reticle shape", ref shape, ShapeLabels, ShapeLabels.Length))
        {
            settings.ReticleStyle = (ReticleStyle)(shape + 1);
            save();
        }
        if (settings.ReticleStyle == ReticleStyle.GameIcon)
            DrawIcon(settings);
        if (settings.ReticleStyle == ReticleStyle.Preset)
            DrawPreset(settings);

        var size = settings.ReticleSize;
        if (ImGui.SliderFloat("Reticle size", ref size, 4, 160, "%.0f"))
        {
            settings.ReticleSize = size;
            save();
        }
        var rotation = settings.ReticleRotation;
        if (ImGui.SliderFloat("Rotation", ref rotation, -180, 180, "%.0f°"))
        {
            settings.ReticleRotation = rotation;
            save();
        }
        var normalColor = settings.ReticleColor;
        if (ImGui.ColorEdit4("Normal color", ref normalColor))
        {
            settings.ReticleColor = normalColor;
            save();
        }
        var targetColor = settings.TargetReticleColor;
        if (ImGui.ColorEdit4("Target color", ref targetColor))
        {
            settings.TargetReticleColor = targetColor;
            save();
        }
        ConfigWindow.DrawTooltip("Used when an eligible target is under the reticle. Colors tint the original image.");
        var animate = settings.AnimateReticle;
        if (ImGui.Checkbox("Animate on target", ref animate))
        {
            settings.AnimateReticle = animate;
            save();
        }
        var depthEnabled = settings.ReticleDepthEnabled;
        if (ImGui.Checkbox("Hide reticle behind scenery", ref depthEnabled))
        {
            settings.ReticleDepthEnabled = depthEnabled;
            save();
        }
        ConfigWindow.DrawTooltip("Uses scene depth to hide covered parts of the reticle. Some effects and transparent surfaces may not cover it correctly. Disable for a normal overlay.");
        using (ImRaii.Disabled(!settings.ReticleDepthEnabled))
        {
            var distance = settings.ReticleDistance;
            if (ImGui.SliderFloat("Reticle distance", ref distance, 0.1f, 100, "%.1f m", ImGuiSliderFlags.Logarithmic))
            {
                settings.ReticleDistance = Math.Clamp(distance, 0.1f, 100);
                save();
            }
            ConfigWindow.DrawTooltip("Distance from the camera for scenery occlusion. Does not change reticle size, aim position, or targeting range.");
        }
        ImGui.Checkbox("Preview target", ref _previewTarget);
    }

    private void DrawIcon(TargetingSettings settings)
    {
        var index = Array.IndexOf(ReticleAssets.IconIds, settings.ReticleIconId);
        if (_customIcon || index < 0) index = ReticleAssets.IconIds.Length;
        if (ImGui.Combo("Icon", ref index, IconLabels, IconLabels.Length))
        {
            _customIcon = index == ReticleAssets.IconIds.Length;
            if (!_customIcon)
            {
                settings.ReticleIconId = ReticleAssets.IconIds[index];
                save();
            }
        }
        ConfigWindow.DrawTooltip($"Icon {settings.ReticleIconId}");
        if (index != ReticleAssets.IconIds.Length) return;
        var iconId = (int)settings.ReticleIconId;
        if (ImGui.InputInt("Icon ID", ref iconId))
        {
            settings.ReticleIconId = (uint)Math.Clamp(iconId, 1, 999999);
            save();
        }
    }

    private void DrawPreset(TargetingSettings settings)
    {
        var design = settings.ReticleDesign - 1;
        if (ImGui.Combo("Design", ref design, DesignLabels, DesignLabels.Length))
        {
            settings.ReticleDesign = design + 1;
            save();
        }
        var normal = settings.NormalVariant;
        if (ImGui.Combo("Normal variant", ref normal, VariantLabels, VariantLabels.Length))
        {
            settings.NormalVariant = normal;
            save();
        }
        var variant = settings.TargetVariant;
        if (ImGui.Combo("Target variant", ref variant, VariantLabels, VariantLabels.Length))
        {
            settings.TargetVariant = variant;
            save();
        }
    }
}
