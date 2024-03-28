// Author: rstewa · https://github.com/rstewa
// Created: 3/28/2024
// Updated: 3/28/2024

using System;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.UserControls;

public sealed partial class CustomHyperlinkButton : UserControl
{
    public event EventHandler? ButtonClick;
    
    public static readonly DependencyProperty ButtonClickProperty =
        DependencyProperty.Register("ButtonClick", typeof(EventHandler), typeof(CustomHyperlinkButton), new PropertyMetadata(null));
    
    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register("Icon", typeof(string), typeof(CustomHyperlinkButton), new PropertyMetadata(null));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(CustomHyperlinkButton), new PropertyMetadata(null));
    
    public CustomHyperlinkButton()
    {
        InitializeComponent();
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        ButtonClick?.Invoke(this, EventArgs.Empty);
    }
}