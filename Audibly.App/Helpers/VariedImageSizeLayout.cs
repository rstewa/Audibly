// Author: rstewa · https://github.com/rstewa
// Created: 3/24/2024
// Updated: 3/24/2024

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Windows.Foundation;
using VirtualizingLayout = Microsoft.UI.Xaml.Controls.VirtualizingLayout;
using VirtualizingLayoutContext = Microsoft.UI.Xaml.Controls.VirtualizingLayoutContext;

namespace Audibly.App.Helpers;

public class VariedImageSizeLayout : VirtualizingLayout
{
    public double Width { get; set; } = 150;

    protected override void OnItemsChangedCore(VirtualizingLayoutContext context, object source,
        NotifyCollectionChangedEventArgs args)
    {
        // The data collection has changed, so the bounds of all the indices are not valid anymore. 
        // We need to re-evaluate all the bounds and cache them during the next measure.
        m_cachedBounds.Clear();
        m_firstIndex = m_lastIndex = 0;
        cachedBoundsInvalid = true;
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(VirtualizingLayoutContext context, Size availableSize)
    {
        var viewport = context.RealizationRect;

        if (availableSize.Width != m_lastAvailableWidth || cachedBoundsInvalid)
        {
            UpdateCachedBounds(availableSize);
            m_lastAvailableWidth = availableSize.Width;
        }

        // Initialize column offsets
        var numColumns = Math.Max(1, (int)(availableSize.Width / Width));
        if (m_columnOffsets.Count == 0)
            for (var i = 0; i < numColumns; i++)
                m_columnOffsets.Add(0);

        m_firstIndex = GetStartIndex(viewport);
        var currentIndex = m_firstIndex;
        var nextOffset = -1.0;

        // Measure items from start index to when we hit the end of the viewport.
        while (currentIndex < context.ItemCount && nextOffset < viewport.Bottom)
        {
            var child = context.GetOrCreateElementAt(currentIndex);
            child.Measure(new Size(Width, availableSize.Height));

            if (currentIndex >= m_cachedBounds.Count)
            {
                // We do not have bounds for this index. Lay it out and cache it.
                var columnIndex = GetIndexOfLowestColumn(m_columnOffsets, out nextOffset);
                m_cachedBounds.Add(new Rect(columnIndex * Width, nextOffset, Width, child.DesiredSize.Height));
                m_columnOffsets[columnIndex] += child.DesiredSize.Height;
            }
            else
            {
                if (currentIndex + 1 == m_cachedBounds.Count)
                    // Last element. Use the next offset.
                    GetIndexOfLowestColumn(m_columnOffsets, out nextOffset);
                else
                    nextOffset = m_cachedBounds[currentIndex + 1].Top;
            }

            m_lastIndex = currentIndex;
            currentIndex++;
        }

        var extent = GetExtentSize(availableSize);
        return extent;
    }

    protected override Size ArrangeOverride(VirtualizingLayoutContext context, Size finalSize)
    {
        if (m_cachedBounds.Count > 0)
            for (var index = m_firstIndex; index <= m_lastIndex; index++)
            {
                var child = context.GetOrCreateElementAt(index);
                child.Arrange(m_cachedBounds[index]);
            }

        return finalSize;
    }

    private void UpdateCachedBounds(Size availableSize)
    {
        var numColumns = Math.Max(1, (int)(availableSize.Width / Width));
        m_columnOffsets.Clear();
        for (var i = 0; i < numColumns; i++) m_columnOffsets.Add(0);

        for (var index = 0; index < m_cachedBounds.Count; index++)
        {
            var columnIndex = GetIndexOfLowestColumn(m_columnOffsets, out var nextOffset);
            var oldHeight = m_cachedBounds[index].Height;
            m_cachedBounds[index] = new Rect(columnIndex * Width, nextOffset, Width, oldHeight);
            m_columnOffsets[columnIndex] += oldHeight;
        }

        cachedBoundsInvalid = false;
    }

    private int GetStartIndex(Rect viewport)
    {
        var startIndex = 0;
        if (m_cachedBounds.Count == 0)
            startIndex = 0;
        else
            // find first index that intersects the viewport
            // perhaps this can be done more efficiently than walking
            // from the start of the list.
            for (var i = 0; i < m_cachedBounds.Count; i++)
            {
                var currentBounds = m_cachedBounds[i];
                if (currentBounds.Y < viewport.Bottom &&
                    currentBounds.Bottom > viewport.Top)
                {
                    startIndex = i;
                    break;
                }
            }

        return startIndex;
    }

    private int GetIndexOfLowestColumn(List<double> columnOffsets, out double lowestOffset)
    {
        var lowestIndex = 0;
        lowestOffset = columnOffsets[lowestIndex];
        for (var index = 0; index < columnOffsets.Count; index++)
        {
            var currentOffset = columnOffsets[index];
            if (lowestOffset > currentOffset)
            {
                lowestOffset = currentOffset;
                lowestIndex = index;
            }
        }

        return lowestIndex;
    }

    private Size GetExtentSize(Size availableSize)
    {
        var largestColumnOffset = m_columnOffsets[0];
        for (var index = 0; index < m_columnOffsets.Count; index++)
        {
            var currentOffset = m_columnOffsets[index];
            if (largestColumnOffset < currentOffset) largestColumnOffset = currentOffset;
        }

        return new Size(availableSize.Width, largestColumnOffset);
    }

    private int m_firstIndex;
    private int m_lastIndex;
    private double m_lastAvailableWidth;
    private readonly List<double> m_columnOffsets = new();
    private readonly List<Rect> m_cachedBounds = new();
    private bool cachedBoundsInvalid;
}