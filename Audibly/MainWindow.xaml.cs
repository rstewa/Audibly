//   Author: Ryan Stewart
//   Date: 05/20/2022

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Audibly.Extensions;
using Audibly.Model;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;

namespace Audibly;

public sealed partial class MainWindow
{
    // public const int WM_NCLBUTTONDOWN = 0xA1;
    // public const int HTCAPTION = 0x2;
    //
    // [DllImport("User32.dll")]
    // public static extern bool ReleaseCapture();
    //
    // [DllImport("User32.dll")]
    // public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    
    public MainWindow()
    {
        // setting MainWindow properties
        InitializeComponent();
        this.SetWindowSize(315, 440, false, false, true, false);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DefaultAppTitleBar);
        
        // TODO -> use a bind in the xaml
        TogglePlayerView();

        AudiobookViewModel.Audiobook.ViewChanged += AudiobookOnViewChanged;
        
        // MainGrid.PointerPressed += MainGridOnPointerPressed;
    }

    // private void MainGridOnPointerPressed(object sender, PointerRoutedEventArgs e)
    // {
    //     var ptr = e.Pointer;
    //     if (ptr.PointerDeviceType == PointerDeviceType.Mouse)
    //     {
    //         // To get mouse state, we need extended pointer details.
    //         // We get the pointer info through the getCurrentPoint method
    //         // of the event argument. 
    //         var ptrPt = e.GetCurrentPoint(null);
    //         if (ptrPt.Properties.IsLeftButtonPressed)
    //         {
    //             ReleaseCapture();
    //             var hWnd = WindowNative.GetWindowHandle(this);
    //             _ = SendMessage(hWnd, WM_NCLBUTTONDOWN, HTCAPTION, 0);
    //         }
    //     }
    // }

    private void TogglePlayerView(Visibility defaultVisibility = Visibility.Visible)
    {
        if (defaultVisibility == Visibility.Visible)
        {
            DefaultGrid.Visibility = Visibility.Visible;
            CompactViewGrid.Visibility = Visibility.Collapsed;
            return;
        }

        DefaultGrid.Visibility = Visibility.Collapsed;
        CompactViewGrid.Visibility = Visibility.Visible;
    }

    private void AudiobookOnViewChanged(object sender, EventArgs e)
    {
        if (e is ViewChangedEventArgs { IsCompact: true } args)
        {
            this.SetWindowSize(315, 307, false, false, true, false);
            ExtendsContentIntoTitleBar = true;
            // SetTitleBar(CompactAppTitleBar);
            TogglePlayerView(Visibility.Collapsed);
        }
        else
        {
            this.SetWindowSize(315, 440, false, false, true, false);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DefaultAppTitleBar);
            TogglePlayerView();
            this.SetWindowTransparent();
        }
    }
}