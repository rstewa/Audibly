// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

// Created: TODO
// Updated: 3/21/2024

using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using static Audibly.App.Helpers.Win32;

namespace Audibly.App.Helpers;

internal class Win32WindowHelper
{
    private static WinProc newWndProc;
    private static nint oldWndProc = nint.Zero;

    private POINT? minWindowSize;
    private POINT? maxWindowSize;

    private readonly Window window;

    public Win32WindowHelper(Window window)
    {
        this.window = window;
    }

    public void SetWindowMinMaxSize(POINT? minWindowSize = null, POINT? maxWindowSize = null)
    {
        this.minWindowSize = minWindowSize;
        this.maxWindowSize = maxWindowSize;

        var hwnd = GetWindowHandleForCurrentWindow(window);

        newWndProc = WndProc;
        oldWndProc = SetWindowLongPtr(hwnd, WindowLongIndexFlags.GWL_WNDPROC, newWndProc);
    }

    private static nint GetWindowHandleForCurrentWindow(object target)
    {
        return WindowNative.GetWindowHandle(target);
    }

    private nint WndProc(nint hWnd, WindowMessage Msg, nint wParam, nint lParam)
    {
        switch (Msg)
        {
            case WindowMessage.WM_GETMINMAXINFO:
                var dpi = GetDpiForWindow(hWnd);
                var scalingFactor = (float)dpi / 96;

                var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                if (minWindowSize != null)
                {
                    minMaxInfo.ptMinTrackSize.x = (int)(minWindowSize.Value.x * scalingFactor);
                    minMaxInfo.ptMinTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                }

                if (maxWindowSize != null)
                {
                    minMaxInfo.ptMaxTrackSize.x = (int)(maxWindowSize.Value.x * scalingFactor);
                    minMaxInfo.ptMaxTrackSize.y = (int)(minWindowSize.Value.y * scalingFactor);
                }

                Marshal.StructureToPtr(minMaxInfo, lParam, true);
                break;
        }

        return CallWindowProc(oldWndProc, hWnd, Msg, wParam, lParam);
    }

    private nint SetWindowLongPtr(nint hWnd, WindowLongIndexFlags nIndex, WinProc newProc)
    {
        if (nint.Size == 8)
            return SetWindowLongPtr64(hWnd, nIndex, newProc);
        return new nint(SetWindowLong32(hWnd, nIndex, newProc));
    }

    internal struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }
}