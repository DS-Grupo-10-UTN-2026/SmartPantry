using System;

namespace SmartPantry.ExternalProducts;

public class ExternalCatalogRateLimitException : Exception
{
    public ExternalCatalogRateLimitException()
        : base("El catálogo externo limitó temporalmente las consultas.")
    {
    }
}