//   Author: Ryan Stewart
//   Date: 03/13/2023

using System;

namespace Audibly.Model;

public class ViewChangedEventArgs : EventArgs
{
    public bool IsCompact { get; set; }
}