// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Helpers;

public class FileSystemItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate FolderTemplate { get; set; }
    public DataTemplate AudiobookTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is CollectionViewModel) return FolderTemplate;
        if (item is AudiobookViewModel) return AudiobookTemplate;
        return base.SelectTemplateCore(item);
    }
}