using System.Runtime.InteropServices;

namespace Lumiere.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct PHYSICAL_MONITOR
{
    public IntPtr hPhysicalMonitor;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
    public string szPhysicalMonitorDescription;
}

[StructLayout(LayoutKind.Sequential)]
public struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public static class VcpCodes
{
    public const byte Brightness = 0x10;
    public const byte Contrast = 0x12;
}

public static class HotkeyModifiers
{
    public const uint None = 0x0000;
    public const uint Alt = 0x0001;
    public const uint Control = 0x0002;
    public const uint Shift = 0x0004;
    public const uint Win = 0x0008;
    public const uint NoRepeat = 0x4000;
}

public static class VirtualKeys
{
    public const int Up = 0x26;
    public const int Down = 0x28;
    public const int Left = 0x25;
    public const int Right = 0x27;
    public const int D = 0x44;
    public const int N = 0x4E;
}

public static class WindowMessages
{
    public const int WM_HOTKEY = 0x0312;
}
