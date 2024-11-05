// Author: rstewa · https://github.com/rstewa
// Created: 08/21/2024
// Updated: 08/21/2024

namespace Audibly.Models;

public class SelectedFile
{
    public SelectedFile(string fileName, string filePath)
    {
        FileName = fileName;
        FilePath = filePath;
    }

    public string FileName { get; set; }
    public string FilePath { get; set; }
}