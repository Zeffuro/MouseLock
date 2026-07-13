using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class CursorVisibilityState
{
    private bool _restoreVisible;

    public bool IsActive { get; private set; }

    public void Apply()
    {
        var stage = AtkStage.Instance();
        if (stage is null)
        {
            Reset();
            return;
        }

        var cursor = &stage->AtkCursor;

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

        var stage = AtkStage.Instance();
        if (stage is not null && _restoreVisible)
        {
            stage->AtkCursor.SetVisible(true);
        }

        Reset();
    }

    private void Reset()
    {
        _restoreVisible = false;
        IsActive = false;
    }
}
