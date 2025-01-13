using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class TitleArtistStack : UserControl
{
    public TitleArtistStack()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;
    
    // title font size dependency property
    public static readonly DependencyProperty TitleFontSizeProperty = DependencyProperty.Register(
        nameof(TitleFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(16.0));
    
    public double TitleFontSize
    {
        get => (double) GetValue(TitleFontSizeProperty);
        set => SetValue(TitleFontSizeProperty, value);
    }
    
    // artist font size dependency property
    public static readonly DependencyProperty ArtistFontSizeProperty = DependencyProperty.Register(
        nameof(ArtistFontSize), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(14.0));
    
    public double ArtistFontSize
    {
        get => (double) GetValue(ArtistFontSizeProperty);
        set => SetValue(ArtistFontSizeProperty, value);
    }
    
    // title max width dependency property
    public static readonly DependencyProperty TitleMaxWidthProperty = DependencyProperty.Register(
        nameof(TitleMaxWidth), typeof(double), typeof(TitleArtistStack), new PropertyMetadata(230.0));
    
    public double TitleMaxWidth
    {
        get => (double) GetValue(TitleMaxWidthProperty);
        set => SetValue(TitleMaxWidthProperty, value);
    }
}