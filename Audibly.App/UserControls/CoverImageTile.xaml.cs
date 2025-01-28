// Author: rstewa · https://github.com/rstewa
// Updated: 01/26/2025

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

    public static readonly DependencyProperty ShowShadowProperty = DependencyProperty.Register(
        nameof(ShowShadow), typeof(bool), typeof(CoverImageTile), new PropertyMetadata(true));

    public CoverImageTile()
    {
        InitializeComponent();
    }

    /// <summary>
    ///     Gets the app-wide PlayerViewModel instance.
    /// </summary>
    public PlayerViewModel PlayerViewModel => App.PlayerViewModel;

    // show shadow dependency property
    public bool ShowShadow
    {
        get => (bool)GetValue(ShowShadowProperty);
        set => SetValue(ShowShadowProperty, value);
    }

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