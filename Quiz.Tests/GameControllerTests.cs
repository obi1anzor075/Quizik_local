extern alias PL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccessLayer.DataContext;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace PresentationLayer.Tests
{
    public class GameControllerTests
    {
        // Вспомогательный метод для создания поддельного DbSet из списка объектов
        private static DbSet<T> CreateDbSetMock<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSetMock = new Mock<DbSet<T>>();
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSetMock.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            return dbSetMock.Object;
        }

        // Метод для настройки поддельного HttpContext с куками
        private static DefaultHttpContext CreateHttpContext(string currentQuestionIndex = "0", string correctAnswersCount = "0")
        {
            var context = new DefaultHttpContext();

            // Мокаем коллекцию Request.Cookies
            var requestCookiesMock = new Mock<IRequestCookieCollection>();
            requestCookiesMock.Setup(x => x["CurrentQuestionIndex"]).Returns(currentQuestionIndex);
            requestCookiesMock.Setup(x => x["CorrectAnswersCount"]).Returns(correctAnswersCount);
            context.Request.Cookies = requestCookiesMock.Object;

            // Response.Cookies оставляем дефолтным (в тестах не проверяем их изменение)
            return context;
        }

        // Вспомогательный метод для получения значения свойства isCorrect из анонимного объекта
        private static bool GetIsCorrect(object result)
        {
            var prop = result.GetType().GetProperty("isCorrect");
            if (prop != null)
            {
                return (bool)prop.GetValue(result);
            }
            throw new Exception("Свойство isCorrect не найдено в результате");
        }

        [Fact]
        public async Task CheckAnswer_CorrectAnswer_ReturnsTrue()
        {
            // Подготавливаем список вопросов для категории "Test"
            var questions = new List<Question>
            {
                new Question
                {
                    QuestionId = 1,
                    Category = "Test",
                    Answer1 = "Неверно1",
                    Answer2 = "Правильный", // правильный ответ, так как CorrectAnswerIndex = 2
                    Answer3 = "Неверно2",
                    Answer4 = "Неверно3",
                    CorrectAnswerIndex = 2
                }
            };

            // Создаём поддельный DbSet для вопросов
            var mockQuestionSet = CreateDbSetMock(questions);

            // Мокаем контекст базы данных, чтобы вернуть поддельный набор вопросов
            var mockContext = new Mock<DataStoreDbContext>();
            mockContext.Setup(c => c.Questions).Returns(mockQuestionSet);

            // Создаём экземпляр контроллера с поддельным контекстом
            var controller = new PL.PresentationLayer.Controllers.GameController(mockContext.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            };

            // Вызываем метод CheckAnswer, передаём gameMode ("EasyTest" преобразуется в "Test")
            // и выбранный ответ "Правильный"
            var result = await controller.CheckAnswer("EasyTest", "Правильный");

            // Проверяем, что результат имеет тип JsonResult
            var jsonResult = Assert.IsType<JsonResult>(result);
            // Получаем значение свойства isCorrect через reflection
            bool isCorrect = GetIsCorrect(jsonResult.Value);
            Assert.True(isCorrect);
        }

        [Fact]
        public async Task CheckAnswer_WrongAnswer_ReturnsFalse()
        {
            // Подготавливаем список вопросов для категории "Test"
            var questions = new List<Question>
            {
                new Question
                {
                    QuestionId = 1,
                    Category = "Test",
                    Answer1 = "Неверно1",
                    Answer2 = "Правильный", // правильный ответ
                    Answer3 = "Неверно2",
                    Answer4 = "Неверно3",
                    CorrectAnswerIndex = 2
                }
            };

            var mockQuestionSet = CreateDbSetMock(questions);

            var mockContext = new Mock<DataStoreDbContext>();
            mockContext.Setup(c => c.Questions).Returns(mockQuestionSet);

            var controller = new PL.PresentationLayer.Controllers.GameController(mockContext.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            };

            // Передаём неверный ответ
            var result = await controller.CheckAnswer("EasyTest", "Неверно1");

            var jsonResult = Assert.IsType<JsonResult>(result);
            bool isCorrect = GetIsCorrect(jsonResult.Value);
            Assert.False(isCorrect);
        }

        [Fact]
        public async Task CheckHardAnswer_CorrectAnswer_ReturnsTrue()
        {
            // Подготавливаем список сложных вопросов для категории "Test"
            var hardQuestions = new List<HardQuestion>
            {
                new HardQuestion
                {
                    QuestionId = 1,
                    Category = "Test",
                    CorrectAnswer = "ОтветА",
                    CorrectAnswer2 = "ОтветБ"
                }
            };

            var mockHardQuestionSet = CreateDbSetMock(hardQuestions);

            var mockContext = new Mock<DataStoreDbContext>();
            mockContext.Setup(c => c.HardQuestions).Returns(mockHardQuestionSet);

            var controller = new PL.PresentationLayer.Controllers.GameController(mockContext.Object);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext()
            };

            // Передаём один из корректных ответов (например, "ОтветА")
            var result = await controller.CheckHardAnswer("HardTest", "ОтветА");

            var jsonResult = Assert.IsType<JsonResult>(result);
            bool isCorrect = GetIsCorrect(jsonResult.Value);
            Assert.True(isCorrect);
        }

        [Fact]
        public void ResetCounters_DeletesCookies_ReturnsOk()
        {
            // Мокаем контекст базы данных (не используется в методе ResetCounters)
            var mockContext = new Mock<DataStoreDbContext>();

            var controller = new PL.PresentationLayer.Controllers.GameController(mockContext.Object);
            // Устанавливаем начальные значения куков через CreateHttpContext
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext("5", "3")
            };

            // Вызываем метод сброса счётчиков
            var result = controller.ResetCounters();

            // Проверяем, что возвращается OkResult
            Assert.IsType<OkResult>(result);
        }
    }
}
