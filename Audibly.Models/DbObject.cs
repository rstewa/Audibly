// Author: rstewa · https://github.com/rstewa
// Created: 3/5/2024
// Updated: 3/22/2024

namespace Audibly.Models;

/// <summary>
///     Base class for database entities.
/// </summary>
public class DbObject
{
    /// <summary>
    ///     Gets or sets the database id.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
}