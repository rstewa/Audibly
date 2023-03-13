using System;

namespace Audibly.Model;

public class ViewChangedEventArgs : EventArgs
{
    public bool IsCompact { get; set; }
}