using FFXIVClientStructs.FFXIV.Component.GUI;

namespace MouseLock.MouseLook.Native;

internal sealed unsafe class CursorVisibilityState
{
    private bool _restoreVisible;

    public bool IsActive { get; private set; }

    public void Apply()
    {
        var cursor = AtkStage.Instance()->AtkCursor;

        if (!IsActive)
        {
            _restoreVisible = cursor.IsVisible;
            IsActive = true;
        }

        if (cursor.IsVisible)
        {
            cursor.SetVisible(false);
        }
    }

    public void Release()
    {
        if (!IsActive)
        {
            return;
        }

        var cursor = AtkStage.Instance()->AtkCursor;
        if (_restoreVisible)
        {
            cursor.SetVisible(true);
        }

        Reset();
    }

    private void Reset()
    {
        _restoreVisible = false;
        IsActive = false;
    }
}
