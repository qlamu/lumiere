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
    // Arrow keys
    public const int Left = 0x25;
    public const int Up = 0x26;
    public const int Right = 0x27;
    public const int Down = 0x28;

    // Numbers 0-9
    public const int D0 = 0x30;
    public const int D1 = 0x31;
    public const int D2 = 0x32;
    public const int D3 = 0x33;
    public const int D4 = 0x34;
    public const int D5 = 0x35;
    public const int D6 = 0x36;
    public const int D7 = 0x37;
    public const int D8 = 0x38;
    public const int D9 = 0x39;

    // Letters A-Z
    public const int A = 0x41;
    public const int B = 0x42;
    public const int C = 0x43;
    public const int D = 0x44;
    public const int E = 0x45;
    public const int F = 0x46;
    public const int G = 0x47;
    public const int H = 0x48;
    public const int I = 0x49;
    public const int J = 0x4A;
    public const int K = 0x4B;
    public const int L = 0x4C;
    public const int M = 0x4D;
    public const int N = 0x4E;
    public const int O = 0x4F;
    public const int P = 0x50;
    public const int Q = 0x51;
    public const int R = 0x52;
    public const int S = 0x53;
    public const int T = 0x54;
    public const int U = 0x55;
    public const int V = 0x56;
    public const int W = 0x57;
    public const int X = 0x58;
    public const int Y = 0x59;
    public const int Z = 0x5A;

    // Function keys F1-F12
    public const int F1 = 0x70;
    public const int F2 = 0x71;
    public const int F3 = 0x72;
    public const int F4 = 0x73;
    public const int F5 = 0x74;
    public const int F6 = 0x75;
    public const int F7 = 0x76;
    public const int F8 = 0x77;
    public const int F9 = 0x78;
    public const int F10 = 0x79;
    public const int F11 = 0x7A;
    public const int F12 = 0x7B;

    // Special keys
    public const int Tab = 0x09;
    public const int Space = 0x20;
    public const int Enter = 0x0D;
    public const int Escape = 0x1B;
    public const int Back = 0x08;
    public const int Delete = 0x2E;
    public const int Insert = 0x2D;
    public const int Home = 0x24;
    public const int End = 0x23;
    public const int PageUp = 0x21;
    public const int PageDown = 0x22;

    // Numpad
    public const int NumPad0 = 0x60;
    public const int NumPad1 = 0x61;
    public const int NumPad2 = 0x62;
    public const int NumPad3 = 0x63;
    public const int NumPad4 = 0x64;
    public const int NumPad5 = 0x65;
    public const int NumPad6 = 0x66;
    public const int NumPad7 = 0x67;
    public const int NumPad8 = 0x68;
    public const int NumPad9 = 0x69;
    public const int NumPadMultiply = 0x6A;
    public const int NumPadAdd = 0x6B;
    public const int NumPadSubtract = 0x6D;
    public const int NumPadDecimal = 0x6E;
    public const int NumPadDivide = 0x6F;

    // OEM keys
    public const int OemPlus = 0xBB;
    public const int OemMinus = 0xBD;
    public const int OemComma = 0xBC;
    public const int OemPeriod = 0xBE;
}

public static class WindowMessages
{
    public const int WM_HOTKEY = 0x0312;
}
