// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/13/2024

using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static PInvoke.User32;

namespace Audibly.App.Extensions;

public static class WindowExtensions
{
    public static void CustomizeWindow(this Window window, int width, int height, bool centerWindow,
        bool extendsContentIntoTitleBar, bool isResizable, bool isMinimizable, bool isMaximizable,
        bool getSizeFromDisplay = false)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        var scalingFactor = (double)GetDpiForWindow(hWnd) / 96;
        if (Math.Abs(scalingFactor - 1.0) > 0)
        {
            width = Math.Ceiling(width * scalingFactor).ToInt();
            height = Math.Ceiling(height * scalingFactor).ToInt();
        }

        var x = 0;
        var y = 0;

        if (getSizeFromDisplay)
        {
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            if (displayArea is not null)
            {
                width = (displayArea.WorkArea.Width * 0.75).ToInt();
                height = (displayArea.WorkArea.Height * 0.75).ToInt();
            }
        }

        if (centerWindow)
        {
            var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            if (displayArea is not null)
            {
                x = (displayArea.WorkArea.Width - width) / 2;
                y = (displayArea.WorkArea.Height - height) / 2;
            }
        }

        appWindow.MoveAndResize(new RectInt32(x, y, width, height));

        window.ExtendsContentIntoTitleBar = extendsContentIntoTitleBar;

        presenter!.IsResizable = isResizable;
        presenter!.IsMaximizable = isMaximizable;
        presenter!.IsMinimizable = isMinimizable;
    }

    public static void CenterWindow(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var displayArea = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);

        if (displayArea is not null)
        {
            var x = (displayArea.WorkArea.Width - appWindow.Size.Width) / 2;
            var y = (displayArea.WorkArea.Height - appWindow.Size.Height) / 2;

            appWindow.Move(new PointInt32(x, y));
        }
    }

    public static void Maximize(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        presenter.Maximize();
    }

    public static void MakeWindowFullScreen(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
        presenter!.SetBorderAndTitleBar(false, false);
    }

    public static void RestoreWindow(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        presenter!.SetBorderAndTitleBar(true, true);
    }

    #region PInvoke Stuff

    public const int WM_CREATE = 0x0001;
    public const int WM_NCHITTEST = 0x0084;
    public const int WM_COMMAND = 0x0111;
    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int WM_NCLBUTTONUP = 0x00A2;
    public const int WM_NCLBUTTONDBLCLK = 0x00A3;
    public const int WM_NCRBUTTONDOWN = 0x00A4;
    public const int WM_NCRBUTTONUP = 0x00A5;
    public const int WM_MOUSEMOVE = 0x0200;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_LBUTTONDBLCLK = 0x0203;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_RBUTTONDBLCLK = 0x0206;
    public const int WM_MOUSEFIRST = 0x0200;
    public const int WM_MOUSELAST = 0x020E;
    public const int WM_SYSCOMMAND = 0x0112;
    public const int WM_MOVE = 0x0003;
    public const int WM_NCPOINTERDOWN = 0x0242;
    public const int WM_NCPOINTERUP = 0x0243;
    public const int WM_POINTERUPDATE = 0x0245;
    public const int WM_POINTERDOWN = 0x0246;
    public const int WM_POINTERUP = 0x0247;
    public const int WM_DESTROY = 2;
    public const int WM_PAINT = 0x0f;

    public const int WS_THICKFRAME = 0x00040000;
    public const int WS_CHILD = 0x40000000;
    public const int WS_POPUP = unchecked((int)0x80000000);

    public const int SC_MOVE = 0xF010;
    public const int SC_MOUSEMOVE = SC_MOVE + 0x02;

    public const int HTERROR = -2;
    public const int HTTRANSPARENT = -1;
    public const int HTNOWHERE = 0;
    public const int HTCLIENT = 1;
    public const int HTCAPTION = 2;
    public const int HTSYSMENU = 3;
    public const int HTGROWBOX = 4;
    public const int HTLEFT = 10;
    public const int HTRIGHT = 11;
    public const int HTTOP = 12;
    public const int HTTOPLEFT = 13;
    public const int HTTOPRIGHT = 14;
    public const int HTBOTTOM = 15;
    public const int HTBOTTOMLEFT = 16;
    public const int HTBOTTOMRIGHT = 17;

    public const int WS_EX_DLGMODALFRAME = 0x00000001;
    public const int WS_EX_NOPARENTNOTIFY = 0x00000004;
    public const int WS_EX_TOPMOST = 0x00000008;
    public const int WS_EX_ACCEPTFILES = 0x00000010;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_MDICHILD = 0x00000040;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_WINDOWEDGE = 0x00000100;
    public const int WS_EX_CLIENTEDGE = 0x00000200;
    public const int WS_EX_CONTEXTHELP = 0x00000400;
    public const int WS_EX_RIGHT = 0x00001000;
    public const int WS_EX_LEFT = 0x00000000;
    public const int WS_EX_RTLREADING = 0x00002000;
    public const int WS_EX_LTRREADING = 0x00000000;
    public const int WS_EX_LEFTSCROLLBAR = 0x00004000;
    public const int WS_EX_RIGHTSCROLLBAR = 0x00000000;
    public const int WS_EX_CONTROLPARENT = 0x00010000;
    public const int WS_EX_STATICEDGE = 0x00020000;
    public const int WS_EX_APPWINDOW = 0x00040000;
    public const int WS_EX_OVERLAPPEDWINDOW = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
    public const int WS_EX_PALETTEWINDOW = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOINHERITLAYOUT = 0x00100000; // Disable inheritence of mirroring by children
    public const int WS_EX_NOREDIRECTIONBITMAP = 0x00200000;
    public const int WS_EX_LAYOUTRTL = 0x00400000; // Right to left mirroring
    public const int WS_EX_COMPOSITED = 0x02000000;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const uint LWA_COLORKEY = 0x00000001;
    public const uint LWA_ALPHA = 0x00000002;

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    private static long MakeArgb(byte alpha, byte red, byte green, byte blue)
    {
        return (long)((ulong)((red << 0x10) | (green << 8) | blue | (alpha << 0x18)) & 0xffffffffL);
    }

    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;

    public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 4) return SetWindowLongPtr32(hWnd, nIndex, dwNewLong);
        return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
    }

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLong")]
    public static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("User32.dll", CharSet = CharSet.Auto, EntryPoint = "SetWindowLongPtr")]
    public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    // public static IntPtr GetWindowLong(HandleRef hWnd, int nIndex)
    public static long GetWindowLong(IntPtr hWnd, int nIndex)
    {
        if (IntPtr.Size == 4) return GetWindowLong32(hWnd, nIndex);
        return GetWindowLongPtr64(hWnd, nIndex);
    }

    [DllImport("User32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
    public static extern long GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("User32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
    public static extern long GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool DestroyWindow(IntPtr hWnd);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool ShowWindow(IntPtr hWnd, int nShowCmd);

    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_SHOWNOACTIVATE = 4;
    public const int SW_SHOW = 5;

    [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

    [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int PostMessage(IntPtr hWnd, uint msg, int wParam, IntPtr lParam);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern int DefWindowProc(IntPtr hWnd, uint uMsg, int wParam, IntPtr lParam);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int iMode);

    [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

    [DllImport("Gdi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

    public const int RGN_AND = 1;
    public const int RGN_OR = 2;
    public const int RGN_XOR = 3;
    public const int RGN_DIFF = 4;
    public const int RGN_COPY = 5;
    public const int RGN_MIN = RGN_AND;
    public const int RGN_MAX = RGN_COPY;

    public const int ERROR = 0;
    public const int NULLREGION = 1;
    public const int SIMPLEREGION = 2;
    public const int COMPLEXREGION = 3;

    [DllImport("Gdi32.dll", SetLastError = true)]
    public static extern bool DeleteObject(IntPtr hObject);

    public delegate int SUBCLASSPROC(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass,
        uint dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    public static extern bool
        SetWindowSubclass(IntPtr hWnd, SUBCLASSPROC pfnSubclass, uint uIdSubclass, uint dwRefData);

    [DllImport("Comctl32.dll", SetLastError = true)]
    public static extern int DefSubclassProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass,
        string lpszWindow);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public RECT(int Left, int Top, int Right, int Bottom)
        {
            left = Left;
            top = Top;
            right = Right;
            bottom = Bottom;
        }
    }

    public static int GET_X_LPARAM(IntPtr lParam)
    {
        return LOWORD(lParam.ToInt32());
    }

    public static int GET_Y_LPARAM(IntPtr lParam)
    {
        return HIWORD(lParam.ToInt32());
    }

    public static int HIWORD(int i)
    {
        return (short)(i >> 16);
    }

    public static int LOWORD(int i)
    {
        return (short)(i & 0xFFFF);
    }

    private static int MakeLParam(int LoWord, int HiWord)
    {
        var res = (HiWord << 16) | (LoWord & 0xffff);
        return res;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;

        public POINT(int X, int Y)
        {
            x = X;
            y = Y;
        }
    }

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool PtInRect(ref RECT lprc, POINT pt);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool ReleaseCapture();

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int cx, int cy, bool repaint);

    public const int SWP_NOSIZE = 0x0001;
    public const int SWP_NOMOVE = 0x0002;
    public const int SWP_NOZORDER = 0x0004;
    public const int SWP_NOREDRAW = 0x0008;
    public const int SWP_NOACTIVATE = 0x0010;
    public const int SWP_FRAMECHANGED = 0x0020; /* The frame changed: send WM_NCCALCSIZE */
    public const int SWP_SHOWWINDOW = 0x0040;
    public const int SWP_HIDEWINDOW = 0x0080;
    public const int SWP_NOCOPYBITS = 0x0100;
    public const int SWP_NOOWNERZORDER = 0x0200; /* Don't do owner Z ordering */
    public const int SWP_NOSENDCHANGING = 0x0400; /* Don't send WM_WINDOWPOSCHANGING */
    public const int SWP_DRAWFRAME = SWP_FRAMECHANGED;
    public const int SWP_NOREPOSITION = SWP_NOOWNERZORDER;
    public const int SWP_DEFERERASE = 0x2000;
    public const int SWP_ASYNCWINDOWPOS = 0x4000;

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy,
        uint uFlags);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    public const int RDW_INVALIDATE = 0x0001;
    public const int RDW_INTERNALPAINT = 0x0002;
    public const int RDW_ERASE = 0x0004;

    public const int RDW_VALIDATE = 0x0008;
    public const int RDW_NOINTERNALPAINT = 0x0010;
    public const int RDW_NOERASE = 0x0020;

    public const int RDW_NOCHILDREN = 0x0040;
    public const int RDW_ALLCHILDREN = 0x0080;

    public const int RDW_UPDATENOW = 0x0100;
    public const int RDW_ERASENOW = 0x0200;

    public const int RDW_FRAME = 0x0400;
    public const int RDW_NOFRAME = 0x0800;

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr hWnd, string lpText, string lpCaption, uint uType);

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern short GetAsyncKeyState(int nVirtKey);

    public const int VK_LBUTTON = 0x01;
    public const int VK_RBUTTON = 0x02;

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

    public const int MOUSEEVENTF_MOVE = 0x0001; /* mouse move */
    public const int MOUSEEVENTF_LEFTDOWN = 0x0002; /* left button down */
    public const int MOUSEEVENTF_LEFTUP = 0x0004; /* left button up */
    public const int MOUSEEVENTF_RIGHTDOWN = 0x0008; /* right button down */
    public const int MOUSEEVENTF_RIGHTUP = 0x0010; /* right button up */
    public const int MOUSEEVENTF_MIDDLEDOWN = 0x0020; /* middle button down */
    public const int MOUSEEVENTF_MIDDLEUP = 0x0040; /* middle button up */
    public const int MOUSEEVENTF_XDOWN = 0x0080; /* x button down */
    public const int MOUSEEVENTF_XUP = 0x0100; /* x button down */
    public const int MOUSEEVENTF_WHEEL = 0x0800; /* wheel button rolled */
    public const int MOUSEEVENTF_HWHEEL = 0x01000; /* hwheel button rolled */
    public const int MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000; /* do not coalesce mouse moves */
    public const int MOUSEEVENTF_VIRTUALDESK = 0x4000; /* map to entire virtual desktop */
    public const int MOUSEEVENTF_ABSOLUTE = 0x8000; /* absolute move */

    [DllImport("User32.dll", SetLastError = true)]
    public static extern int SendInput(int nInputs, [MarshalAs(UnmanagedType.LPArray)] INPUT[] pInput, int cbSize);

    public const int INPUT_MOUSE = 0;
    public const int INPUT_KEYBOARD = 1;
    public const int INPUT_HARDWARE = 2;

    public const int KEYEVENTF_EXTENDEDKEY = 0x0001;
    public const int KEYEVENTF_KEYUP = 0x0002;
    public const int KEYEVENTF_UNICODE = 0x0004;

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public int mouseData;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public short wVk;
        public short wScan;
        public int dwFlags;
        public int time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public int uMsg;
        public short wParamL;
        public short wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public INPUTUNION inputUnion;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [DllImport("User32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("User32.dll", SetLastError = true)]
    public static extern IntPtr GetCapture();

    [DllImport("User32.dll", SetLastError = true)]
    public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    [DllImport("User32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr CallWindowProc(IntPtr wndProc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    //[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [DllImport("User32.dll", SetLastError = true, EntryPoint = "RegisterClassW")]
    public static extern short RegisterClass(ref WNDCLASS wc);

    [DllImport("User32.dll", SetLastError = true, EntryPoint = "RegisterClassExW")]
    public static extern short RegisterClassEx(ref WNDCLASSEX lpwcx);

    public delegate int WNDPROC(IntPtr hwnd, uint uMsg, int wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WNDCLASS
    {
        [MarshalAs(UnmanagedType.U4)] public uint style;
        public WNDPROC lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string lpszMenuName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string lpszClassName;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct WNDCLASSEX
    {
        [MarshalAs(UnmanagedType.U4)] public int cbSize;
        [MarshalAs(UnmanagedType.U4)] public int style;
        public WNDPROC lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    public const int WS_OVERLAPPEDWINDOW = 0xcf0000;
    public const int WS_VISIBLE = 0x10000000;
    public const int CS_USEDEFAULT = unchecked((int)0x80000000);
    public const int CS_DBLCLKS = 8;
    public const int CS_VREDRAW = 1;
    public const int CS_HREDRAW = 2;
    public const int COLOR_BACKGROUND = 1;
    public const int COLOR_WINDOW = 5;
    public const int IDC_ARROW = 32512;
    public const int IDC_IBEAM = 32513;
    public const int IDC_WAIT = 32514;
    public const int IDC_CROSS = 32515;
    public const int IDC_UPARROW = 32516;

    public const int BS_PUSHLIKE = 0x00001000;
    public const int BN_CLICKED = 0;

    [DllImport("User32.dll", SetLastError = true)]
    public static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr GetModuleHandle(string modName);

    #endregion

    public static void SetWindowOpacity(this Window window, int nOpacity, bool removeBorderAndTitleBar = false)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);
        var presenter = appWindow.Presenter as OverlappedPresenter;

        if (removeBorderAndTitleBar)
            presenter!.SetBorderAndTitleBar(false, false);

        var nExStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, (IntPtr)(nExStyle | WS_EX_LAYERED));
        SetLayeredWindowAttributes(hWnd, 0, (byte)(255 * nOpacity / 100), LWA_ALPHA);
        nExStyle = GetWindowLong(hWnd, GWL_EXSTYLE);
        SetWindowLong(hWnd, GWL_EXSTYLE, (IntPtr)(nExStyle | WS_EX_APPWINDOW));
    }

    public static int Width(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        return appWindow.Size.Width;
    }

    public static int Height(this Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        var appWindow = AppWindow.GetFromWindowId(windowId);

        return appWindow.Size.Height;
    }
}