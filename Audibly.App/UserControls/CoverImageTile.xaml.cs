using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class CoverImageTile : UserControl
{
    public static readonly DependencyProperty CoverImageSizeProperty = DependencyProperty.Register(
        nameof(CoverImageSize),
        typeof(double),
        typeof(CoverImageTile),
        new PropertyMetadata(116.0));

    public static readonly DependencyProperty CoverImageRadiusProperty = DependencyProperty.Register(
        nameof(CoverImageRadius),
        typeof(double),
        typeof(CoverImageTile),
        new PropertyMetadata(10));

    public CoverImageTile()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    public double CoverImageSize
    {
        get => (double)GetValue(CoverImageSizeProperty);
        set => SetValue(CoverImageSizeProperty, value);
    }

    public double CoverImageRadius
    {
        get => (double)GetValue(CoverImageRadiusProperty);
        set => SetValue(CoverImageRadiusProperty, value);
    }
}