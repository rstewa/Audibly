// Author: rstewa · https://github.com/rstewa
// Created: 4/8/2024
// Updated: 4/13/2024

using Audibly.App.Helpers;

namespace Audibly.App.Services;

public class MessageService
{
    public delegate void ShowDialogDelegate(DialogType type, string title, string content);

    public event ShowDialogDelegate ShowDialogRequested;

    public delegate void CancelDialogDelegate();

    public event CancelDialogDelegate CancelDialogRequested;

    public void ShowDialog(DialogType type, string title, string content)
    {
        ShowDialogRequested?.Invoke(type, title, content);
    }

    public void CancelDialog()
    {
        CancelDialogRequested?.Invoke();
    }
}