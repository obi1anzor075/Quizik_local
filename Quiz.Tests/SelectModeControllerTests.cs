using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using PresentationLayer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

public class SelectModeControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SelectModeControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EasyPDD_ReturnsViewWithQuestion()
    {
        // Act
        var response = await _client.GetAsync("/SelectMode/EasyPDD");

        // Assert
        response.EnsureSuccessStatusCode(); // Проверяем, что статус код 200
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("QuestionText", content); // Проверяем, что возвращается страница с вопросом
    }
}