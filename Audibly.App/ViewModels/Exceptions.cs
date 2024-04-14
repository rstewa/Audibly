// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;

namespace Audibly.App.ViewModels;

/// <summary>
///     Represents an exception that occurs when there's an error saving an order.
/// </summary>
public class OrderSavingException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the OrderSavingException class with a default error message.
    /// </summary>
    public OrderSavingException() : base("Error saving an order.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the OrderSavingException class with the specified error message.
    /// </summary>
    public OrderSavingException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the OrderSavingException class with
    ///     the specified error message and inner exception.
    /// </summary>
    public OrderSavingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     Represents an exception that occurs when there's an error deleting an order.
/// </summary>
public class OrderDeletionException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the OrderDeletionException class with a default error message.
    /// </summary>
    public OrderDeletionException() : base("Error deleting an order.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the OrderDeletionException class with the specified error message.
    /// </summary>
    public OrderDeletionException(string message) : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the OrderDeletionException class with
    ///     the specified error message and inner exception.
    /// </summary>
    public OrderDeletionException(string message,
        Exception innerException) : base(message, innerException)
    {
    }
}