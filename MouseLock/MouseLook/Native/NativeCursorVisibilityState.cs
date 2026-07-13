using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class NativeCursorVisibilityState
{
    private bool _restoreVisible;

    public bool IsActive { get; private set; }

    public void Apply()
    {
        var cursor = GetCursor();
        if (cursor is null)
        {
            return;
        }

        if (!IsActive)
        {
            _restoreVisible = cursor->IsVisible;
            IsActive = true;
        }

        if (cursor->IsVisible)
        {
            cursor->SetVisible(false);
        }
    }

    public void Release()
    {
        if (!IsActive)
        {
            return;
        }

        var cursor = GetCursor();
        if (cursor is not null && _restoreVisible)
        {
            cursor->SetVisible(true);
        }

        Reset();
    }

    public void Reset()
    {
        _restoreVisible = false;
        IsActive = false;
    }

    private AtkCursor* GetCursor()
    {
        var stage = AtkStage.Instance();
        return stage is null ? null : &stage->AtkCursor;
    }
}
