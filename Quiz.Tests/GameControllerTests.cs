using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using PresentationLayer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

public class GameControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GameControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CheckAnswer_ReturnsJsonResult()
    {
        // Arrange
        var gameMode = "EasyPDD";
        var selectedAnswer = "CorrectAnswer";

        // Act
        var response = await _client.GetAsync($"/Game/CheckAnswer/{gameMode}/{selectedAnswer}");

        // Assert
        response.EnsureSuccessStatusCode(); // Проверяем, что статус код 200
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("isCorrect", content); // Проверяем, что ответ содержит JSON с полем isCorrect
    }
}