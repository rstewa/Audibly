using System;
using System.Diagnostics;
using Windows.Storage.Pickers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT.Interop;

namespace Audibly;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // play/pause button disabled until an audio file is successfully opened
        PlayPauseButton.IsEnabled = false;
    }

    private void AppBarButton_Click(object sender, RoutedEventArgs e)
    {
        Debug.WriteLine("commandbar button pressed");
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        if ((string) PlayPauseButton.Tag == "play")
        {
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Pause);
            PlayPauseButton.Label = "Pause";
            PlayPauseButton.Tag = "pause";
        }
        else
        {
            PlayPauseButton.Icon = new SymbolIcon(Symbol.Play);
            PlayPauseButton.Label = "Play";
            PlayPauseButton.Tag = "play";
        }
    }

    private async void OpenAudioFile(object sender, RoutedEventArgs e)
    {
        // Create the file picker
        var filePicker = new FileOpenPicker();

        // Get the current window's HWND by passing in the Window object
        var hwnd = WindowNative.GetWindowHandle(this);

        // Associate the HWND with the file picker
        InitializeWithWindow.Initialize(filePicker, hwnd);

        // Use file picker like normal!
        filePicker.FileTypeFilter.Add(".m4a");
        var file = await filePicker.PickSingleFileAsync();

        Debug.WriteLine($"[Class=MainWindow][Method=SetLocalMedia] file name: {file.Name}");
    }
}