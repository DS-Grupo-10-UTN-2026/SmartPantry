namespace SmartPantry.ExternalProducts;

public enum ProductLookupStatus
{
    Found = 1,
    NotFound = 2,
    RateLimited = 3,
    ServiceUnavailable = 4
}