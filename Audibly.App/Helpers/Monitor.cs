// Author: rstewa · https://github.com/rstewa
// Created: 01/14/2025
// Updated: 01/14/2025

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Audibly.App.Helpers;

public sealed class Monitor
{
    #region MFW enum

    [Flags]
    public enum MFW
    {
        MONITOR_DEFAULTTONULL = 0x00000000,
        MONITOR_DEFAULTTOPRIMARY = 0x00000001,
        MONITOR_DEFAULTTONEAREST = 0x00000002
    }

    #endregion

    #region MONITORINFOF enum

    [Flags]
    public enum MONITORINFOF
    {
        MONITORINFOF_NONE = 0x00000000,
        MONITORINFOF_PRIMARY = 0x00000001
    }

    #endregion

    private Monitor(IntPtr handle)
    {
        Handle = handle;
        var mi = new MONITORINFOEX();
        mi.cbSize = Marshal.SizeOf(mi);
        if (!GetMonitorInfo(handle, ref mi))
            throw new Win32Exception(Marshal.GetLastWin32Error());

        DeviceName = mi.szDevice;
        Bounds = new RectInt32(mi.rcMonitor.left, mi.rcMonitor.top, mi.rcMonitor.right - mi.rcMonitor.left,
            mi.rcMonitor.bottom - mi.rcMonitor.top);
        WorkingArea = new RectInt32(mi.rcWork.left, mi.rcWork.top, mi.rcWork.right - mi.rcWork.left,
            mi.rcWork.bottom - mi.rcWork.top);
        IsPrimary = mi.dwFlags.HasFlag(MONITORINFOF.MONITORINFOF_PRIMARY);
    }

    public IntPtr Handle { get; }
    public bool IsPrimary { get; }
    public RectInt32 WorkingArea { get; }
    public RectInt32 Bounds { get; }
    public string DeviceName { get; }

    public static IEnumerable<Monitor> All
    {
        get
        {
            var all = new List<Monitor>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (m, h, rc, p) =>
            {
                all.Add(new Monitor(m));
                return true;
            }, IntPtr.Zero);
            return all;
        }
    }

    public override string ToString()
    {
        return DeviceName;
    }

    public static IntPtr GetNearestFromWindow(IntPtr hwnd)
    {
        return MonitorFromWindow(hwnd, MFW.MONITOR_DEFAULTTONEAREST);
    }

    public static IntPtr GetDesktopMonitorHandle()
    {
        return GetNearestFromWindow(GetDesktopWindow());
    }

    public static IntPtr GetShellMonitorHandle()
    {
        return GetNearestFromWindow(GetShellWindow());
    }

    public static Monitor FromWindow(IntPtr hwnd, MFW flags = MFW.MONITOR_DEFAULTTONULL)
    {
        var h = MonitorFromWindow(hwnd, flags);
        return h != IntPtr.Zero ? new Monitor(h) : null;
    }

    [DllImport("user32")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32")]
    private static extern IntPtr GetShellWindow();

    [DllImport("user32")]
    private static extern bool
        EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, MFW flags);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hmonitor, ref MONITORINFOEX info);

    #region Nested type: MonitorEnumProc

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr lParam);

    #endregion

    #region Nested type: MONITORINFOEX

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public MONITORINFOF dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    #endregion

    #region Nested type: RECT

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
    }

    #endregion
}