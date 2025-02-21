using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using PresentationLayer;              // пространство имен вашего основного проекта
using PresentationLayer.ViewModels;   // для LoginVM, RegisterVM

namespace Quiz.Tests.Integration
{
    // WebApplicationFactory<Program> ожидает, что в вашем основном проекте есть класс Program
    public class HomeControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public HomeControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            // Опционально можно настроить WebHostBuilder, если требуется дополнительная конфигурация для тестов
            _client = factory.CreateClient();
        }

        /// <summary>
        /// Проверяет, что GET-запрос к /Home/Login возвращает страницу входа.
        /// </summary>
        [Fact]
        public async Task Get_Login_Returns_LoginView()
        {
            // Act
            var response = await _client.GetAsync("/Home/Login");

            // Assert
            response.EnsureSuccessStatusCode(); // ожидаем статус 200 OK
            var content = await response.Content.ReadAsStringAsync();
            // Предполагаем, что на странице входа встречается слово "Login" или иной маркер (в зависимости от вашей разметки)
            Assert.Contains("Login", content, System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Тестирует выход из системы – после logout пользователь перенаправляется на страницу Login.
        /// Для тестирования logout необходимо, чтобы пользователь был аутентифицирован.
        /// Здесь для упрощения теста сначала выполняем регистрацию (которая автоматически логинит пользователя),
        /// затем вызываем Logout.
        /// </summary>
        [Fact]
        public async Task Logout_RedirectsTo_Login()
        {
            // Arrange: Регистрируем пользователя, чтобы он был аутентифицирован.
            var registerVM = new RegisterVM
            {
                Email = "logoutuser@example.com",
                UserName = "LogoutUser",
                Password = "ComplexPass!123",
                ConfirmPassword = "ComplexPass!123"
            };

            var regResponse = await _client.PostAsJsonAsync("/Home/Register", registerVM);
            regResponse.EnsureSuccessStatusCode();

            // Act: Вызываем Logout
            var response = await _client.GetAsync("/Home/Logout");

            // Assert:
            // При вызове Logout происходит перенаправление на Login.
            if (response.StatusCode == HttpStatusCode.Redirect)
            {
                var redirectUri = response.Headers.Location?.AbsolutePath;
                Assert.Equal("/Home/Login", redirectUri);
            }
            else
            {
                // Если редирект выполняется автоматически, проверяем наличие маркера "Login" в контенте
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                Assert.Contains("Login", content, System.StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Проверяем, что метод GetUserName возвращает пустое значение, если cookie не установлено.
        /// </summary>
        [Fact]
        public async Task GetUserName_Returns_Empty_WhenCookieNotSet()
        {
            // Act: обращаемся к GetUserName
            var response = await _client.GetAsync("/Home/GetUserName");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            // Ожидаем, что возвращается JSON с userName равным пустой строке.
            Assert.Contains("\"userName\":\"\"", content);
        }
    }
}
