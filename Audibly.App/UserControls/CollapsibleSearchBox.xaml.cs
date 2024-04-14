// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Audibly.App.UserControls;

public sealed partial class CollapsibleSearchBox : UserControl
{
    public CollapsibleSearchBox()
    {
        InitializeComponent();
        Loaded += CollapsableSearchBox_Loaded;
        Unloaded += CollapsibleSearchBox_Unloaded;
        App.Window.SizeChanged += Current_SizeChanged;
        AutoSuggestBox = SearchBox;
    }

    public double CollapseWidth
    {
        get => (double)GetValue(CollapseWidthProperty);
        set => SetValue(CollapseWidthProperty, value);
    }

    // Using a DependencyProperty as the backing store for CollapseWidth.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty CollapseWidthProperty =
        DependencyProperty.Register("CollapseWidth", typeof(double), typeof(CollapsibleSearchBox),
            new PropertyMetadata(0.0));

    public AutoSuggestBox AutoSuggestBox { get; private set; }

    private void CollapsableSearchBox_Loaded(object sender, RoutedEventArgs e)
    {
        SearchButton.AddHandler(PointerPressedEvent,
            new PointerEventHandler(ToggleButton_PointerPressed), true);
        SearchButton.AddHandler(PointerReleasedEvent,
            new PointerEventHandler(ToggleButton_PointerReleased), true);

        SetState(App.Window.Bounds.Width);
    }

    private void CollapsibleSearchBox_Unloaded(object sender, RoutedEventArgs e)
    {
        SearchButton.RemoveHandler(PointerPressedEvent,
            (PointerEventHandler)ToggleButton_PointerPressed);
        SearchButton.RemoveHandler(PointerReleasedEvent,
            (PointerEventHandler)ToggleButton_PointerReleased);
    }

    private void Current_SizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        SetState(e.Size.Width);
    }

    private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
    {
        SetState(App.Window.Bounds.Width);
        SearchButton.IsChecked = false;
    }

    private void SetState(double width)
    {
        if (width <= CollapseWidth)
            VisualStateManager.GoToState(this, "CollapsedState", true);
        else
            VisualStateManager.GoToState(this, "OpenState", true);
    }

    private void SearchButton_Checked(object sender, RoutedEventArgs e)
    {
        VisualStateManager.GoToState(this, "OpenState", true);
        if (SearchBox != null) SearchBox.Focus(FocusState.Programmatic);
    }

    // Set states for animated icon in toggle button.
    private void ToggleButton_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState((UIElement)sender, "PointerOver");
    }

    private void ToggleButton_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState((UIElement)sender, "Pressed");
    }

    private void ToggleButton_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState((UIElement)sender, "Normal");
    }

    private void ToggleButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        AnimatedIcon.SetState((UIElement)sender, "Normal");
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(sender.Text))
            VisualStateManager.GoToState(this, "NonFilteredState", true);
        else
            VisualStateManager.GoToState(this, "FilteredState", true);
    }
}