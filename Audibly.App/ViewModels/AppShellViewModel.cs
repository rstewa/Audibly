// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using System.Collections.ObjectModel;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

public class AppShellViewModel : BindableBase
{
    private ObservableCollection<MenuItemViewModel> _menuItems = new();

    public AppShellViewModel()
    {
        // Initialize the MenuItems collection with some initial items
        var libraryMenuItem = new MenuItemViewModel
        {
            Content = "Library",
            Tag = "LibraryPage",
            Icon = Symbol.Home
        };

        LoadLibrarySubMenuItems(libraryMenuItem);

        // MenuItems.Add(libraryMenuItem);
        //
        // MenuItems.Add(new MenuItemViewModel { Content = "Settings", Tag = "SettingsPage", Icon = Symbol.Setting });
    }

    private void LoadLibrarySubMenuItems(MenuItemViewModel libraryMenuItem)
    {
        // Simulate loading submenu items dynamically
        libraryMenuItem.SubMenuItems.Add(new MenuItemViewModel
            { Content = "Subitem 1", Tag = "Subitem1Page", Icon = Symbol.Document });
        libraryMenuItem.SubMenuItems.Add(new MenuItemViewModel
            { Content = "Subitem 2", Tag = "Subitem2Page", Icon = Symbol.Document });
    }
}