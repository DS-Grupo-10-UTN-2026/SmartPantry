using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartPantry.ExternalProducts;

public class OpenFoodFactsProductCatalogClientTests
{
    private const string Barcode = "3017620422003";

    private const string ExpectedQuery =
        "?fields=code,product_name,product_name_es,brands,quantity,image_front_url,nutriments";

    [Fact]
    public async Task Should_Request_The_V3_Product_Route_With_Selected_Fields()
    {
        var handler = new StubHandler(_ => Json("""{ "product": { "code": "3017620422003" } }"""));
        var client = CreateClient(handler);

        await client.GetByBarcodeAsync(Barcode);

        var request = handler.LastRequest!;
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("world.openfoodfacts.org", request.RequestUri!.Host);
        Assert.Equal("/api/v3/product/3017620422003", request.RequestUri.AbsolutePath);
        Assert.Equal(ExpectedQuery, Uri.UnescapeDataString(request.RequestUri.Query));
    }

    [Fact]
    public async Task Should_Map_All_Fields_When_Provider_Returns_Complete_Product()
    {
        const string json = """
        {
          "product": {
            "code": "3017620422003",
            "product_name": "Nutella EN",
            "product_name_es": "Nutella",
            "brands": "Nutella, Ferrero",
            "quantity": "400 g",
            "image_front_url": "https://example.com/nutella.jpg",
            "nutriments": {
              "energy-kcal_100g": 539,
              "fat_100g": 30.9,
              "sugars_100g": 56.3,
              "salt_100g": 0.107
            }
          }
        }
        """;
        var client = CreateClient(new StubHandler(_ => Json(json)));

        var result = await client.GetByBarcodeAsync(Barcode);

        Assert.NotNull(result);
        Assert.Equal("3017620422003", result!.Barcode);
        Assert.Equal("Nutella", result.Name); // prefiere el nombre en español
        Assert.Equal("Nutella, Ferrero", result.Brand);
        Assert.Equal("400 g", result.Quantity);
        Assert.Equal("https://example.com/nutella.jpg", result.ImageUrl);
        Assert.Equal(539m, result.EnergyKcalPer100g);
        Assert.Equal(30.9m, result.FatPer100g);
        Assert.Equal(56.3m, result.SugarsPer100g);
        Assert.Equal(0.107m, result.SaltPer100g);
    }

    [Fact]
    public async Task Should_Keep_Missing_Fields_As_Null()
    {
        var client = CreateClient(new StubHandler(_ => Json("""{ "product": { "code": "3017620422003" } }""")));

        var result = await client.GetByBarcodeAsync(Barcode);

        Assert.NotNull(result);
        Assert.Equal(Barcode, result!.Barcode);
        Assert.Null(result.Name);
        Assert.Null(result.Brand);
        Assert.Null(result.Quantity);
        Assert.Null(result.ImageUrl);
        Assert.Null(result.EnergyKcalPer100g);
        Assert.Null(result.FatPer100g);
        Assert.Null(result.SugarsPer100g);
        Assert.Null(result.SaltPer100g);
    }

    [Fact]
    public async Task Should_Treat_Blank_Text_As_Missing_And_Fall_Back_To_Default_Name()
    {
        const string json = """
        {
          "product": {
            "code": "3017620422003",
            "product_name": "Nombre por defecto",
            "product_name_es": "   ",
            "brands": "",
            "nutriments": { "fat_100g": 1.5 }
          }
        }
        """;
        var client = CreateClient(new StubHandler(_ => Json(json)));

        var result = await client.GetByBarcodeAsync(Barcode);

        Assert.NotNull(result);
        Assert.Equal("Nombre por defecto", result!.Name);
        Assert.Null(result.Brand);
        Assert.Equal(1.5m, result.FatPer100g);
        Assert.Null(result.EnergyKcalPer100g); // un nutriente ausente no afecta a los demás
    }

    [Fact]
    public async Task Should_Return_Null_When_Response_Has_No_Product()
    {
        var client = CreateClient(new StubHandler(_ => Json("{}")));

        var result = await client.GetByBarcodeAsync(Barcode);

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Return_Null_On_404()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await client.GetByBarcodeAsync("0000000000000");

        Assert.Null(result);
    }

    [Fact]
    public async Task Should_Throw_RateLimit_Exception_On_429()
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests)));

        await Assert.ThrowsAsync<ExternalCatalogRateLimitException>(
            () => client.GetByBarcodeAsync(Barcode));
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Should_Throw_Unavailable_Exception_On_Provider_Errors(HttpStatusCode status)
    {
        var client = CreateClient(new StubHandler(_ => new HttpResponseMessage(status)));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync(Barcode));
    }

    [Fact]
    public async Task Should_Throw_Unavailable_Exception_When_Json_Is_Invalid()
    {
        var client = CreateClient(new StubHandler(_ => Json("esto no es json")));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync(Barcode));
    }

    [Fact]
    public async Task Should_Throw_Unavailable_Exception_When_Connection_Fails()
    {
        var client = CreateClient(new StubHandler(_ => throw new HttpRequestException("sin conexión")));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync(Barcode));
    }

    [Fact]
    public async Task Should_Throw_Unavailable_Exception_On_Timeout()
    {
        var client = CreateClient(new StubHandler(_ => throw new TaskCanceledException("timeout")));

        await Assert.ThrowsAsync<ExternalCatalogUnavailableException>(
            () => client.GetByBarcodeAsync(Barcode));
    }

    private static OpenFoodFactsProductCatalogClient CreateClient(StubHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://world.openfoodfacts.org/api/v3/")
        };
        return new OpenFoodFactsProductCatalogClient(httpClient);
    }

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}