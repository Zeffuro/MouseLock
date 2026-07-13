namespace MouseLock.Hotbars;

internal readonly record struct HotbarPickerLayout(int Columns, int Rows)
{
    public static HotbarPickerLayout Default => new(6, 2);
}
