// Author: rstewa · https://github.com/rstewa
// Created: 3/29/2024
// Updated: 4/2/2024

using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class AudiobookTile : UserControl
{
    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register("Title", typeof(string), typeof(AudiobookTile), new PropertyMetadata(null));

    public string Author
    {
        get => (string)GetValue(AuthorProperty);
        set => SetValue(AuthorProperty, value);
    }

    public static readonly DependencyProperty AuthorProperty =
        DependencyProperty.Register("Author", typeof(string), typeof(AudiobookTile), new PropertyMetadata(null));

    public object Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(object), typeof(AudiobookTile), new PropertyMetadata(null));

    public AudiobookTile()
    {
        InitializeComponent();
    }

    private void Audiobook_OnClick(object sender, RoutedEventArgs e)
    {
        App.ViewModel.SelectedAudiobook = App.ViewModel.Audiobooks.FirstOrDefault(a => a.Title == Title);
    }
}