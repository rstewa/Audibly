//   Author: Ryan Stewart
//   Date: 05/20/2022

using System;
using Audibly.Extensions;
using Audibly.Model;
using Microsoft.UI.Xaml;

namespace Audibly;

public sealed partial class MainWindow
{
    public MainWindow()
    {
        // setting MainWindow properties
        InitializeComponent();
        this.SetWindowSize(315, 440, false, false, false);
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(DefaultAppTitleBar);
        
        // TODO -> use a bind in the xaml
        TogglePlayerView();

        AudiobookViewModel.Audiobook.ViewChanged += AudiobookOnViewChanged;
    }

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
        if (e is ViewChangedEventArgs { IsCompact: true })
        {
            this.SetWindowSize(315, 307, false, false, false);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(CompactAppTitleBar);
            TogglePlayerView(Visibility.Collapsed);
            this.SetWindowOpacity(65);
        }
        else
        {
            this.SetWindowSize(315, 440, false, false, false);
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(DefaultAppTitleBar);
            TogglePlayerView();
            this.SetWindowOpacity(100);
        }
    }
}