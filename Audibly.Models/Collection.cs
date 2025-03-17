// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Microsoft.EntityFrameworkCore;

namespace Audibly.Models;

[Index(nameof(Name), nameof(ParentFolderId), IsUnique = true)]
public class Collection : DbObject, IEquatable<Collection>
{
    public required string Name { get; set; }

    public Guid? ParentFolderId { get; set; }

    public List<Audiobook> Audiobooks { get; set; } = [];

    #region IEquatable<Collection> Members

    public bool Equals(Collection? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Collection)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }
}