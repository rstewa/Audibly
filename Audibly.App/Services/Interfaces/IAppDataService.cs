// Author: rstewa · https://github.com/rstewa
// Created: 3/31/2024
// Updated: 4/13/2024

using System;
using System.Threading.Tasks;
using ATL;

namespace Audibly.App.Services.Interfaces;

public interface IAppDataService
{
    Task<Tuple<string, string>> WriteCoverImageAsync(string path, string newCoverImagePath);
    Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes);

    Task DeleteCoverImageAsync(string path);
    
    Task WriteMetadataAsync(string path, Track track);
}