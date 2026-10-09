using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SmartPantry.ExternalProducts;

public class OpenFoodFactsProductCatalogClient : IExternalProductCatalogClient
{
    private const string Fields =
        "code,product_name,product_name_es,brands,quantity,image_front_url,nutriments";

    private readonly HttpClient _httpClient;

    public OpenFoodFactsProductCatalogClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExternalProductDto?> GetByBarcodeAsync(string barcode)
    {
        var path = $"product/{Uri.EscapeDataString(barcode)}?fields={Uri.EscapeDataString(Fields)}";

        try
        {
            using var response = await _httpClient.GetAsync(path);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throw new ExternalCatalogRateLimitException();
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalCatalogUnavailableException(
                    $"El catálogo externo respondió HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadFromJsonAsync<OffResponse>();
            if (body?.Product is null)
            {
                return null;
            }

            return Map(barcode, body.Product);
        }
        catch (TaskCanceledException ex)
        {
            throw new ExternalCatalogUnavailableException(
                "La consulta al catálogo externo excedió el tiempo de espera.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalCatalogUnavailableException(
                "No se pudo conectar con el catálogo externo.", ex);
        }
        catch (JsonException ex)
        {
            throw new ExternalCatalogUnavailableException(
                "El catálogo externo devolvió una respuesta que no pudo interpretarse.", ex);
        }
    }

    private static ExternalProductDto Map(string requestedBarcode, OffProduct p)
    {
        return new ExternalProductDto
        {
            Barcode = string.IsNullOrWhiteSpace(p.Code) ? requestedBarcode : p.Code!,
            Name = FirstNonEmpty(p.ProductNameEs, p.ProductName),
            Brand = FirstNonEmpty(p.Brands),
            Quantity = FirstNonEmpty(p.Quantity),
            ImageUrl = FirstNonEmpty(p.ImageFrontUrl),
            EnergyKcalPer100g = p.Nutriments?.EnergyKcal100g,
            FatPer100g = p.Nutriments?.Fat100g,
            SugarsPer100g = p.Nutriments?.Sugars100g,
            SaltPer100g = p.Nutriments?.Salt100g
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

internal class OffResponse
{
    [JsonPropertyName("product")]
    public OffProduct? Product { get; set; }
}

internal class OffProduct
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("product_name")]
    public string? ProductName { get; set; }

    [JsonPropertyName("product_name_es")]
    public string? ProductNameEs { get; set; }

    [JsonPropertyName("brands")]
    public string? Brands { get; set; }

    [JsonPropertyName("quantity")]
    public string? Quantity { get; set; }

    [JsonPropertyName("image_front_url")]
    public string? ImageFrontUrl { get; set; }

    [JsonPropertyName("nutriments")]
    public OffNutriments? Nutriments { get; set; }
}

internal class OffNutriments
{
    [JsonPropertyName("energy-kcal_100g")]
    public decimal? EnergyKcal100g { get; set; }

    [JsonPropertyName("fat_100g")]
    public decimal? Fat100g { get; set; }

    [JsonPropertyName("sugars_100g")]
    public decimal? Sugars100g { get; set; }

    [JsonPropertyName("salt_100g")]
    public decimal? Salt100g { get; set; }
}