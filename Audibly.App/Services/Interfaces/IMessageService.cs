// Author: rstewa · https://github.com/rstewa
// Created: 4/8/2024
// Updated: 4/8/2024

namespace Audibly.App.Services.Interfaces;

public interface IMessageService
{
    void ShowDialog(string title, string message);
    
    public delegate void ShowDialogDelegate(string title, string content);

    public event ShowDialogDelegate ShowDialogRequested;
}