using System;

namespace SmartPantry.ExternalProducts;

public class ExternalCatalogUnavailableException : Exception
{
    public ExternalCatalogUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}