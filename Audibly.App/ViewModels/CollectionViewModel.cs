// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Audibly.App.ViewModels.Interfaces;
using Audibly.Models;

namespace Audibly.App.ViewModels;

public class CollectionViewModel : BindableBase, IFileSystemItem
{
    private Collection _model;

    public CollectionViewModel(Collection? model = null)
    {
        Model = model ?? new Collection { Name = "New Folder" };
    }

    public Collection Model
    {
        get => _model;
        set
        {
            // if (_model.Equals(value)) return;

            _model = value;

            // Raise the PropertyChanged event for all properties.
            IsModified = true;
            OnPropertyChanged(string.Empty);
        }
    }

    /// <summary>
    ///     Gets or sets a value that indicates whether the underlying model has been modified.
    /// </summary>
    /// <remarks>
    ///     Used when sync'ing with the server to reduce load and only upload the models that have changed.
    /// </remarks>
    public bool IsModified { get; set; }

    public Guid? ParentFolderId
    {
        get => Model.ParentFolderId;
        set
        {
            if (Model.ParentFolderId != value)
            {
                Model.ParentFolderId = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    public List<Audiobook> Audiobooks
    {
        get => Model.Audiobooks;
        set
        {
            if (Model.Audiobooks != value)
            {
                Model.Audiobooks = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    #region IFileSystemItem Members

    public string Name
    {
        get => Model.Name;
        set
        {
            if (Model.Name != value)
            {
                Model.Name = value;
                IsModified = true;
                OnPropertyChanged();
            }
        }
    }

    #endregion

    public async Task SaveAsync()
    {
        // Save the folder to the database
        if (IsModified)
            await App.Repository.Collections.UpsertAsync(Model);

        IsModified = false;
    }
}