using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using MouseLock.Configuration;
using MouseLock.Configuration.Persistence;
using MouseLock.MouseLook.Actions;

namespace MouseLock.Windows;

public sealed partial class ConfigWindow
{
    private void DrawMouseActionBinding(string label, MouseButtonGameInputBinding binding)
    {
        ImGui.PushID(label);

        var kindIndex = FindOptionIndex(BindingKindOptions, binding.Kind);
        if (ImGui.Combo($"{label} binding", ref kindIndex, BindingKindLabels, BindingKindLabels.Length))
        {
            binding.Kind = BindingKindOptions[kindIndex].Value;
            binding.Clamp();
            ConfigRepository.Save(_config);
        }

        switch (binding.Kind)
        {
            case MouseButtonBindingKind.GameInput:
                DrawGameInputBinding(binding);
                break;
            case MouseButtonBindingKind.HotbarSlot:
                DrawHotbarSlotBinding(binding);
                break;
        }

        DrawResolvedBinding(binding);

        ImGui.PopID();
    }

    private void DrawGameInputBinding(MouseButtonGameInputBinding binding)
    {
        DrawNestIndicator(1);
        var gameInputIndex = FindOptionIndex(GameInputOptions, binding.GameInput);
        if (ImGui.Combo("Game input", ref gameInputIndex, GameInputLabels, GameInputLabels.Length))
        {
            binding.GameInput = GameInputOptions[gameInputIndex].Value;
            ConfigRepository.Save(_config);
        }
    }

    private void DrawHotbarSlotBinding(MouseButtonGameInputBinding binding)
    {
        DrawNestIndicator(1);
        var hotbar = binding.Hotbar;
        if (ImGui.InputInt("Hotbar", ref hotbar))
        {
            binding.Hotbar = Math.Clamp(hotbar, 1, 10);
            ConfigRepository.Save(_config);
        }

        DrawNestIndicator(1);
        var slot = binding.Slot;
        if (ImGui.InputInt("Slot", ref slot))
        {
            binding.Slot = Math.Clamp(slot, 1, 12);
            ConfigRepository.Save(_config);
        }
    }

    private void DrawResolvedBinding(MouseButtonGameInputBinding binding)
    {
        if (binding.Kind == MouseButtonBindingKind.None)
        {
            return;
        }

        DrawNestIndicator(1);
        if (TryDrawResolvedHotbarIcon(binding))
        {
            ImGui.SameLine();
        }

        ImGui.TextDisabled($"Resolved input: {MouseButtonGameInputResolver.GetDisplayName(binding)}");
    }

    private static bool TryDrawResolvedHotbarIcon(MouseButtonGameInputBinding binding)
    {
        if (binding.Kind != MouseButtonBindingKind.HotbarSlot ||
            !MouseButtonHotbarExecutor.TryGetIconId(binding.Hotbar, binding.Slot, out var iconId))
        {
            return false;
        }

        var lookup = new GameIconLookup(iconId, false, false);
        if (!Service.TextureProvider.TryGetFromGameIcon(lookup, out var sharedTexture) ||
            !sharedTexture.TryGetWrap(out var texture, out _) ||
            texture is null)
        {
            return false;
        }

        var size = new Vector2(ImGui.GetFrameHeight());
        ImGui.Image(texture.Handle, size);
        DrawTooltip(
            MouseButtonHotbarExecutor.TryGetDisplayName(binding.Hotbar, binding.Slot, out var displayName)
                ? displayName
                : $"Icon {iconId}");
        return true;
    }
}
