// Author: rstewa · https://github.com/rstewa
// Created: 11/30/2024
// Updated: 11/30/2024

using Windows.Storage;
using ATL;
using Audibly.Models;

namespace Audibly.App.Extensions;

public static class StorageFileExtensions
{
    public static AudiobookSearchParameters GetAudiobookSearchParameters(this IStorageFile file)
    {
        var track = new Track(file.Path);
        return new AudiobookSearchParameters { Title = track.Title, Author = track.Artist, Composer = track.Composer };
    }
}