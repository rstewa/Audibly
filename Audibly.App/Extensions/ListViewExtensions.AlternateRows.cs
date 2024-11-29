// Author: rstewa · https://github.com/rstewa
// Created: 11/28/2024
// Updated: 11/28/2024

using System.Collections.Generic;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Audibly.App.Extensions;

// TODO: remove this once they update the CommunityToolkit.WinUI package

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/// <summary>
///     Provides attached dependency properties for the <see cref="ListViewBase" />
/// </summary>
public static class ListViewExtensions
{
    private static readonly Dictionary<IObservableVector<object>, ListViewBase> _itemsForList = new();

    public static readonly DependencyProperty AlternateColorProperty =
        DependencyProperty.RegisterAttached("AlternateColor", typeof(Brush), typeof(ListViewExtensions),
            new PropertyMetadata(null, OnAlternateColorPropertyChanged));

    public static readonly DependencyProperty AlternateItemTemplateProperty =
        DependencyProperty.RegisterAttached("AlternateItemTemplate", typeof(DataTemplate), typeof(ListViewExtensions),
            new PropertyMetadata(null, OnAlternateItemTemplatePropertyChanged));

    public static readonly DependencyProperty PrimaryColorProperty = DependencyProperty.RegisterAttached("PrimaryColor",
        typeof(Brush), typeof(ListViewExtensions), new PropertyMetadata(null, OnPrimaryColorPropertyChanged));

    public static Brush GetAlternateColor(ListViewBase obj)
    {
        return (Brush)obj.GetValue(AlternateColorProperty);
    }

    public static void SetAlternateColor(ListViewBase obj, Brush value)
    {
        obj.SetValue(AlternateColorProperty, value);
    }

    public static DataTemplate GetAlternateItemTemplate(ListViewBase obj)
    {
        return (DataTemplate)obj.GetValue(AlternateItemTemplateProperty);
    }

    public static void SetAlternateItemTemplate(ListViewBase obj, DataTemplate value)
    {
        obj.SetValue(AlternateItemTemplateProperty, value);
    }

    public static Brush GetPrimaryColor(ListViewBase obj)
    {
        return (Brush)obj.GetValue(PrimaryColorProperty);
    }

    public static void SetPrimaryColor(ListViewBase obj, Brush value)
    {
        obj.SetValue(PrimaryColorProperty, value);
    }

    private static void OnAlternateColorPropertyChanged(DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ColorContainerContentChanging;
            listViewBase.Items.VectorChanged -= ColorItemsVectorChanged;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            _itemsForList[listViewBase.Items] = listViewBase;
            if (AlternateColorProperty != null)
            {
                listViewBase.ContainerContentChanging += ColorContainerContentChanging;
                listViewBase.Items.VectorChanged += ColorItemsVectorChanged;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void OnPrimaryColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase) ReapplyAlternateColors(listViewBase);
    }

    private static void ColorContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        var itemContainer = args.ItemContainer as Control;
        SetItemContainerBackground(sender, itemContainer, args.ItemIndex);
    }

    private static void OnAlternateItemTemplatePropertyChanged(DependencyObject sender,
        DependencyPropertyChangedEventArgs args)
    {
        if (sender is ListViewBase listViewBase)
        {
            listViewBase.ContainerContentChanging -= ItemTemplateContainerContentChanging;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;

            if (AlternateItemTemplateProperty != null)
            {
                listViewBase.ContainerContentChanging += ItemTemplateContainerContentChanging;
                listViewBase.Unloaded += OnListViewBaseUnloaded;
            }
        }
    }

    private static void ItemTemplateContainerContentChanging(ListViewBase sender,
        ContainerContentChangingEventArgs args)
    {
        if (args.ItemIndex % 2 == 0)
            args.ItemContainer.ContentTemplate = GetAlternateItemTemplate(sender);
        else
            args.ItemContainer.ContentTemplate = sender.ItemTemplate;
    }

    private static void OnListViewBaseUnloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListViewBase listViewBase)
        {
            _itemsForList.Remove(listViewBase.Items);

            listViewBase.ContainerContentChanging -= ItemTemplateContainerContentChanging;
            listViewBase.ContainerContentChanging -= ColorContainerContentChanging;
            listViewBase.Items.VectorChanged -= ColorItemsVectorChanged;
            listViewBase.Unloaded -= OnListViewBaseUnloaded;
        }
    }

    private static void ColorItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        _itemsForList.TryGetValue(sender, out var listViewBase);
        if (listViewBase == null) return;

        // Reapply alternate colors after reordering
        ReapplyAlternateColors(listViewBase);
    }

    private static void SetItemContainerBackground(ListViewBase sender, Control itemContainer, int itemIndex)
    {
        if (itemIndex % 2 == 0)
        {
            itemContainer.Background = GetAlternateColor(sender);
            var rootBorder = itemContainer.FindDescendant<Border>();
            if (rootBorder != null) rootBorder.Background = GetAlternateColor(sender);
        }
        else
        {
            //itemContainer.Background = null;
            //var rootBorder = itemContainer.FindDescendant<Border>();
            //if (rootBorder != null)
            //{
            //    rootBorder.Background = null;
            //}

            itemContainer.Background = GetPrimaryColor(sender);
            var rootBorder = itemContainer.FindDescendant<Border>();
            if (rootBorder != null) rootBorder.Background = GetPrimaryColor(sender);
        }
    }

    public static void ReapplyAlternateColors(ListViewBase listViewBase)
    {
        for (var i = 0; i < listViewBase.Items.Count; i++)
        {
            var itemContainer = listViewBase.ContainerFromIndex(i) as Control;
            if (itemContainer != null) SetItemContainerBackground(listViewBase, itemContainer, i);
        }
    }
}