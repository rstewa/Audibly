// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.ViewModels;

public class MenuItemViewModel : BindableBase
{
    public string Content { get; set; }
    public string Tag { get; set; }
    public Symbol Icon { get; set; }
    public ObservableCollection<MenuItemViewModel> SubMenuItems { get; set; } = new();
}