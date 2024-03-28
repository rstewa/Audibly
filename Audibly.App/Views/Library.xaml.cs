// Author: rstewa · https://github.com/rstewa
// Created: 3/24/2024
// Updated: 3/24/2024

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Audibly.App.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Audibly.App.Views;

/// <summary>
///     An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class Library : Page
{
    /// <summary>
    ///     Gets the app-wide ViewModel instance.
    /// </summary>
    public MainViewModel ViewModel => App.ViewModel;
    
    public Library()
    {
        InitializeComponent();
        // InitializeData();
    }

    public MyItemsSource filteredRecipeData = new(null);
    public List<Recipe> staticRecipeData;
    private bool IsSortDescending = false;

    private void InitializeData()
    {
        // ...
        // Create a list of Recipe objects, initializing each of them with a random number,
        // correlating name, and random color to associate with it.
        var rnd = new Random();
        var tempList = new List<Recipe>(
            Enumerable.Range(0, 1000).Select(k =>
                new Recipe
                {
                    Num = k,
                    Name = "Recipe " + k,
                    Color = ColorList[rnd.Next(0, 19)]
                }));

        // The lists fruits, vegetables, grains, and proteins were all populated with strings.
        // This loop goes through each Recipe item and populates its ingredients list with one
        // string from each list/category, then randomizes the ingredients list by adding extras.
        foreach (var rec in tempList)
        {
            var fruitOption = GetFruits()[rnd.Next(0, 6)];
            var vegOption = GetVegetables()[rnd.Next(0, 6)];
            var grainOption = GetGrains()[rnd.Next(0, 6)];
            var proteinOption = GetProteins()[rnd.Next(0, 6)];
            rec.Ingredients = "\n" + fruitOption + "\n" + vegOption + "\n" +
                              grainOption + "\n" + proteinOption;
            rec.IngList = new List<string> { fruitOption, vegOption, grainOption, proteinOption };
            rec.RandomizeIngredients();
        }

        // The custom MyItemsSource object, filteredRecipeData, is initialized.
        filteredRecipeData.InitializeCollection(tempList);
        // A static list of the original recipe data is saved to use for filtering.
        staticRecipeData = new List<Recipe>(tempList);
        // The ItemsSource is set for the ItemsRepeater created in the XAML file.
        // VariedImageSizeRepeater.ItemsSource = filteredRecipeData;

        // ...
    }

// ========================== Filtering, sorting, animating ==========================
    public void FilterRecipes_FilterChanged(object sender, RoutedEventArgs e)
    {
        UpdateSortAndFilter();
    }

    private void OnSortAscClick(object sender, RoutedEventArgs e)
    {
        if (IsSortDescending == true)
        {
            IsSortDescending = false;
            UpdateSortAndFilter();
        }
    }

    private void OnSortDesClick(object sender, RoutedEventArgs e)
    {
        if (!IsSortDescending == true)
        {
            IsSortDescending = true;
            UpdateSortAndFilter();
        }
    }

    private void UpdateSortAndFilter()
    {
        // This is a Linq query that fetches all Recipes containing the ingredient
        // typed into the filter text box.
        // var filteredTypes = staticRecipeData.Where(i => i.Ingredients.Contains(
        //     FilterRecipes.Text,
        //     StringComparison.InvariantCultureIgnoreCase));
        // // After filtering, sort the collection
        // var sortedFilteredTypes = IsSortDescending
        //     ? filteredTypes.OrderByDescending(i => i.NumIngredients)
        //     : filteredTypes.OrderBy(i => i.NumIngredients);
        // // Re-initialize the collection with this newly filtered data
        // filteredRecipeData.InitializeCollection(sortedFilteredTypes);
    }

    private ObservableCollection<string> GetFruits()
    {
        return new ObservableCollection<string>
            { "Apricots", "Bananas", "Grapes", "Strawberries", "Watermelon", "Plums", "Blueberries" };
    }


    private ObservableCollection<string> GetVegetables()
    {
        return new ObservableCollection<string>
            { "Broccoli", "Spinach", "Sweet potato", "Cauliflower", "Onion", "Brussels sprouts", "Carrots" };
    }

    private ObservableCollection<string> GetGrains()
    {
        return new ObservableCollection<string> { "Rice", "Quinoa", "Pasta", "Bread", "Farro", "Oats", "Barley" };
    }

    private ObservableCollection<string> GetProteins()
    {
        return new ObservableCollection<string> { "Steak", "Chicken", "Tofu", "Salmon", "Pork", "Chickpeas", "Eggs" };
    }

    public List<string> ColorList = new()
    {
        "Blue",
        "BlueViolet",
        "Crimson",
        "DarkCyan",
        "DarkGoldenrod",
        "DarkMagenta",
        "DarkOliveGreen",
        "DarkRed",
        "DarkSlateBlue",
        "DeepPink",
        "IndianRed",
        "MediumSlateBlue",
        "Maroon",
        "MidnightBlue",
        "Peru",
        "SaddleBrown",
        "SteelBlue",
        "OrangeRed",
        "Firebrick",
        "DarkKhaki"
    };

    private void scroller_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
    {
        // throw new NotImplementedException();
    }

    private void scroller_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // throw new NotImplementedException();
    }
}

public class Recipe
{
    public int Num { get; set; }
    public string Ingredients { get; set; }
    public List<string> IngList { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
    public int NumIngredients => IngList.Count();


    public void RandomizeIngredients()
    {
        // To give the items different heights, give recipes random numbers of random ingredients
        var rndNum = new Random();
        var rndIng = new Random();


        var extras = new ObservableCollection<string>
        {
            "Garlic",
            "Lemon",
            "Butter",
            "Lime",
            "Feta Cheese",
            "Parmesan Cheese",
            "Breadcrumbs"
        };
        for (var i = 0; i < rndNum.Next(0, 4); i++)
        {
            var newIng = extras[rndIng.Next(0, 6)];
            if (!IngList.Contains(newIng))
            {
                Ingredients += "\n" + newIng;
                IngList.Add(newIng);
            }
        }
    }
}

// Custom data source class that assigns elements unique IDs, making filtering easier
public class MyItemsSource : IList, IKeyIndexMapping, INotifyCollectionChanged
{
    private readonly List<Recipe> inner = new();


    public MyItemsSource(IEnumerable<Recipe> collection)
    {
        InitializeCollection(collection);
    }


    public void InitializeCollection(IEnumerable<Recipe> collection)
    {
        inner.Clear();
        if (collection != null) inner.AddRange(collection);


        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }


    #region IReadOnlyList<T>

    public int Count => inner != null ? inner.Count : 0;


    public object this[int index]
    {
        get => inner[index];


        set => inner[index] = (Recipe)value;
    }


    public IEnumerator<Recipe> GetEnumerator()
    {
        return inner.GetEnumerator();
    }

    #endregion


    #region INotifyCollectionChanged

    public event NotifyCollectionChangedEventHandler CollectionChanged;

    #endregion


    #region IKeyIndexMapping

    public string KeyFromIndex(int index)
    {
        return inner[index].Num.ToString();
    }


    public int IndexFromKey(string key)
    {
        foreach (var item in inner)
            if (item.Num.ToString() == key)
                return inner.IndexOf(item);
        return -1;
    }

    #endregion


    #region Unused List methods

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }


    public int Add(object value)
    {
        throw new NotImplementedException();
    }


    public void Clear()
    {
        throw new NotImplementedException();
    }


    public bool Contains(object value)
    {
        throw new NotImplementedException();
    }


    public int IndexOf(object value)
    {
        throw new NotImplementedException();
    }


    public void Insert(int index, object value)
    {
        throw new NotImplementedException();
    }


    public void Remove(object value)
    {
        throw new NotImplementedException();
    }


    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }


    public void CopyTo(Array array, int index)
    {
        throw new NotImplementedException();
    }


    public bool IsFixedSize => throw new NotImplementedException();


    public bool IsReadOnly => throw new NotImplementedException();


    public bool IsSynchronized => throw new NotImplementedException();


    public object SyncRoot => throw new NotImplementedException();

    #endregion
}