using FluentAssertions;
using Infrastructure.ExternalServices;
using Infrastructure.Tests.Unit.Helpers;
using NSubstitute;
using System.Net;
using System.Text.Json;

namespace Infrastructure.Tests.Unit.ExternalServices;

public class CatApiServiceTests
{
    private readonly CatApiService _sut;
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>(); 
    private readonly FakeLogger<CatApiService> _logger;

    public CatApiServiceTests()
    {
        _logger = new FakeLogger<CatApiService>();
        _sut = new CatApiService(_httpClientFactory, _logger);
    }

    private CatApiService CreateSut(HttpResponseMessage response)
    {
        var handler = new FakeHttpMessageHandler(response);
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.thecatapi.com/")
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(CatApiService.HttpClientName).Returns(httpClient);

        

        return new CatApiService(factory, _logger);
    }

    [Fact]
    public async Task FetchCatImagesAsync_ShouldReturnMappedCatApiImages_WhenApiReturnsData()
    {
        // Arrange
        var apiResponse = new[]
        {
            new
            {
                id = "abc123",
                width = 800,
                height = 600,
                url = "https://cdn2.thecatapi.com/images/abc123.jpg",
                breeds = new[]
                {
                    new { temperament = "Playful, Active" }
                }
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.FetchCatImagesAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("abc123");
        result[0].Width.Should().Be(800);
        result[0].Height.Should().Be(600);
        result[0].Url.Should().Be("https://cdn2.thecatapi.com/images/abc123.jpg");
        result[0].Breeds.Should().HaveCount(1);
        result[0].Breeds[0].Temperament.Should().Be("Playful, Active");
    }

    [Fact]
    public async Task FetchCatImagesAsync_ShouldReturnEmptyList_WhenApiReturnsEmptyArray()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]")
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.FetchCatImagesAsync(1, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchCatImagesAsync_ShouldThrow_WhenApiReturnsErrorStatusCode()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

        var sut = CreateSut(response);

        // Act
        var act = () => sut.FetchCatImagesAsync(1, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task FetchCatImagesAsync_ShouldHandleImagesWithoutBreeds()
    {
        // Arrange
        var apiResponse = new[]
        {
            new
            {
                id = "xyz789",
                width = 1024,
                height = 768,
                url = "https://cdn2.thecatapi.com/images/xyz789.jpg",
                breeds = Array.Empty<object>()
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.FetchCatImagesAsync(1, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result[0].Breeds.Should().BeEmpty();
    }

    [Fact]
    public async Task FetchCatImagesAsync_ShouldMapMultipleImages()
    {
        // Arrange
        var apiResponse = new[]
        {
            new
            {
                id = "img1",
                width = 100,
                height = 100,
                url = "https://cdn2.thecatapi.com/images/img1.jpg",
                breeds = Array.Empty<object>()
            },
            new
            {
                id = "img2",
                width = 200,
                height = 200,
                url = "https://cdn2.thecatapi.com/images/img2.jpg",
                breeds = Array.Empty<object>()
            }
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(apiResponse))
        };

        var sut = CreateSut(response);

        // Act
        var result = await sut.FetchCatImagesAsync(2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be("img1");
        result[1].Id.Should().Be("img2");
    }
}
