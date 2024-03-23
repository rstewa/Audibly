// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public partial class ProgressBarCard : UserControl
{
    public static readonly DependencyProperty ProgressBarVisibilityProperty = DependencyProperty.Register(
        "ProgressBarVisibility", typeof(Visibility), typeof(ProgressBarCard),
        new PropertyMetadata(Visibility.Collapsed));

    public Visibility ProgressBarVisibility
    {
        get => (Visibility)GetValue(ProgressBarVisibilityProperty);
        set => SetValue(ProgressBarVisibilityProperty, value);
    }

    public static readonly DependencyProperty ProgressProperty = DependencyProperty.Register(
        "Progress", typeof(double), typeof(ProgressBarCard), new PropertyMetadata(0.0));

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public static readonly DependencyProperty ProgressTextProperty = DependencyProperty.Register(
        "ProgressText", typeof(string), typeof(ProgressBarCard), new PropertyMetadata(string.Empty));

    public string ProgressText
    {
        get => (string)GetValue(ProgressTextProperty);
        set => SetValue(ProgressTextProperty, value);
    }

    public ProgressBarCard()
    {
        InitializeComponent();
    }
}