//   Author: Ryan Stewart
//   Date: 05/20/2022

using Audibly.Extensions;
using Audibly.Model;
using Microsoft.UI.Xaml;
using System;

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
            this.SetWindowOpacity(75); // , removeBorderAndTitleBar: true);
            // this.SetWindowSize(315, 315, false, false, false);
            this.SetWindowSize(285, 285, false, false, false);
            CompactAppTitleBar.Width = (this.Width() / 3).ToDouble();
            DispatcherQueue.TryEnqueue(() =>
            {
                ExtendsContentIntoTitleBar = true;
                TogglePlayerView(Visibility.Collapsed);
                SetTitleBar(CompactAppTitleBar);
            });
        }
        else
        {
            this.SetWindowOpacity(100);
            this.SetWindowSize(315, 440, false, false, false);
            DispatcherQueue.TryEnqueue(() =>
            {
                ExtendsContentIntoTitleBar = true;
                TogglePlayerView();
                SetTitleBar(DefaultAppTitleBar);
            });
        }
    }
}