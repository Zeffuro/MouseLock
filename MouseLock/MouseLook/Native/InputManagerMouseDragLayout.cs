using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace MouseLock.MouseLook.Native;

[StructLayout(LayoutKind.Explicit)]
internal struct InputManagerMouseDragLayout
{
    [FieldOffset(0x28)]
    private int _mouseButtonHoldState;

    public InputManager.MouseButtonHoldState MouseButtonHoldState
    {
        get => (InputManager.MouseButtonHoldState)(byte)_mouseButtonHoldState;
        set => _mouseButtonHoldState = (byte)value;
    }

    [FieldOffset(0x2C)]
    public float MouseDragDistance;

    [FieldOffset(0x30)]
    public int MouseDeltaX;

    [FieldOffset(0x34)]
    public int MouseDeltaY;

    [FieldOffset(0x38)]
    public int MouseDragStartX;

    [FieldOffset(0x3C)]
    public int MouseDragStartY;

    [FieldOffset(0x40)]
    public byte MouseDragActive;
}
