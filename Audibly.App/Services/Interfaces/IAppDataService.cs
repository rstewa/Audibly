// Author: rstewa · https://github.com/rstewa
// Created: 3/31/2024
// Updated: 3/31/2024

using System;
using System.Threading.Tasks;

namespace Audibly.App.Services.Interfaces;

public interface IAppDataService
{
    Task<Tuple<string, string>> WriteCoverImageAsync(string path, byte[]? imageBytes);
    
    Task DeleteCoverImageAsync(string path);
}