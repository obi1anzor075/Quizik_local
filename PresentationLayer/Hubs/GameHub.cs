using Microsoft.AspNetCore.SignalR;
using PresentationLayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using DataAccessLayer.DataContext;
using BusinessLogicLayer.Services.Contracts;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;

namespace PresentationLayer.Hubs
{
    public interface IChatClient
    {
        Task ReceiveMessage(string userName, string message);
        Task StartGame();
        Task EndGame(Dictionary<string, int> results);
        Task GameReady();
        Task ReceiveQuestion(int questionId, string questionText, string imageUrl,string? questionExplanation ,List<string> answers);
        Task AnswerResult(bool isCorrect);
        Task RoomFull();
    }

    public class GameHub : Hub<IChatClient>
    {
        private readonly IDistributedCache _cache;
        private readonly DataStoreDbContext _dbContext;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService;

        private readonly IImageService _imageService;

        private static readonly Dictionary<string, GameRoom> Rooms = new();
        private static readonly Dictionary<string, Timer> RoomTimers = new();

        private static Dictionary<string, Duel> _duelCache = new Dictionary<string, Duel>();
        private static Dictionary<string, PlayerState> _playerStateCache = new Dictionary<string, PlayerState>();


        public GameHub(IDistributedCache cache, DataStoreDbContext dbContext, IHttpContextAccessor httpContextAccessor, IUserService userService, IImageService imageService)
        {
            _cache = cache;
            _dbContext = dbContext;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _imageService = imageService;
        }

        public Task<string> GetUserName()
        {
            var userName = _httpContextAccessor.HttpContext.Request.Cookies["userName"];
            if (string.IsNullOrEmpty(userName))
            {
                throw new KeyNotFoundException("User name not found");
            }

            return Task.FromResult(userName);
        }


        // Save UserName and GET
        /*public async Task SaveUserName()
          {
              var userName = Context.GetHttpContext().Request.Cookies["userName"];
              if (string.IsNullOrEmpty(userName))
              {
                  throw new ArgumentException("Invalid user name");
              }

              // Create user object with the received username
              var user = new User
              {
                  Name = userName,
                  GoogleId = null, // Assuming no GoogleId for non-Google login
                  Email = "user@example.com", // Replace with actual email if available
                  CreatedAt = DateTime.UtcNow
              };

              // Save the user to the database
              await _userService.SaveUserAsync(user);
          }
        */
        public async Task JoinChat(UserConnection connection)
        {
            if (connection == null || string.IsNullOrEmpty(connection.UserName) || string.IsNullOrEmpty(connection.ChatRoom))
            {
                throw new ArgumentException("Invalid connection parameters");
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatRoom);
            await Clients.Group(connection.ChatRoom).ReceiveMessage("Quizik", $"Добро пожаловать, {connection.UserName}!");
        }

        public async Task JoinDuel(UserConnection connection)
        {
            try
            {
                if (connection == null || string.IsNullOrEmpty(connection.UserName) || string.IsNullOrEmpty(connection.ChatRoom))
                {
                    throw new ArgumentException("Invalid connection parameters");
                }

                var duel = await GetOrCreateDuel(connection.ChatRoom);

                if (!string.IsNullOrEmpty(duel.Player1) && !string.IsNullOrEmpty(duel.Player2))
                {
                    await Clients.Caller.RoomFull();
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, connection.ChatRoom);

                if (string.IsNullOrEmpty(duel.Player1))
                {
                    duel = duel with { Player1 = connection.UserName };
                }
                else if (string.IsNullOrEmpty(duel.Player2))
                {
                    duel = duel with { Player2 = connection.UserName };
                }

                await SaveDuel(connection.ChatRoom, duel);

                if (!string.IsNullOrEmpty(duel.Player1) && !string.IsNullOrEmpty(duel.Player2))
                {
                    if (!Rooms.ContainsKey(connection.ChatRoom))
                    {
                        Rooms[connection.ChatRoom] = new GameRoom(connection.ChatRoom);
                    }

                    Rooms[connection.ChatRoom].StartGame();

                    await Clients.Group(connection.ChatRoom).GameReady();
                    await Clients.Group(connection.ChatRoom).StartGame();
                }

                var playerState = await GetPlayerState(connection.UserName);
                if (playerState == null)
                {
                    await SavePlayerState(connection.UserName, new PlayerState());
                    Console.WriteLine($"User {connection.UserName} added to player states.");
                }
                else
                {
                    Console.WriteLine($"User {connection.UserName} already exists in player states.");
                }
            }
            catch (StackExchange.Redis.RedisConnectionException ex)
            {
                Console.WriteLine($"Redis connection error in JoinDuel: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // Add any specific handling or retry logic here if necessary
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinDuel: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        private void StartRoomTimer(string chatRoom)
        {
            if (RoomTimers.ContainsKey(chatRoom))
            {
                RoomTimers[chatRoom].Dispose();
            }

            var timer = new Timer(async _ => await ClearRoom(chatRoom), null, TimeSpan.FromMinutes(10), Timeout.InfiniteTimeSpan);
            RoomTimers[chatRoom] = timer;
        }

        private async Task ClearRoom(string chatRoom)
        {
            await ClearDuel(chatRoom);
            RoomTimers.Remove(chatRoom);
        }

        public async Task SendMessage(string userName, string message)
        {
            await Clients.All.ReceiveMessage(userName, message);
        }

        public async Task AnswerQuestion(string userName, string chatRoom, int questionId, string answer, string gameMode)
        {
            try
            {
                Console.WriteLine($"AnswerQuestion called with UserName={userName}, ChatRoom={chatRoom}, QuestionId={questionId}, Answer={answer}");

                var playerState = await GetPlayerState(userName);
                if (playerState == null)
                {
                    throw new KeyNotFoundException($"User '{userName}' not found in player states.");
                }

                string category = gameMode.Replace("Duel", "");

                Console.WriteLine($"Current Question Index from playerState: {questionId}");

                var question = await _dbContext.Questions
                    .Where(q => q.Category == category)
                    .OrderBy(q => q.QuestionId)
                    .Skip(questionId)
                    .FirstOrDefaultAsync();

                if (question == null)
                {
                    throw new Exception($"Question at index {questionId} not found.");
                }

                string correctAnswer = question.CorrectAnswerIndex switch
                {
                    1 => question.Answer1,
                    2 => question.Answer2,
                    3 => question.Answer3,
                    4 => question.Answer4,
                    _ => string.Empty
                };

                bool isCorrect = string.Equals(answer.Trim(), correctAnswer?.Trim(), StringComparison.OrdinalIgnoreCase);
                if (isCorrect)
                {
                    playerState.Score++;
                }

                await SavePlayerState(userName, playerState);
                await Clients.Caller.AnswerResult(isCorrect);

                int questionCount = await _dbContext.Questions
                    .Where(q => q.Category == category)
                    .CountAsync();

                // Если текущий вопрос — последний, завершаем игру
                if (questionId >= questionCount - 1)
                {
                    await EndGame(chatRoom);
                }
                else
                {
                    // Увеличиваем индекс для следующего вопроса
                    playerState.CurrentQuestionIndex++;
                    questionId++;
                    await SavePlayerState(userName, playerState);
                    await GetNextQuestion(userName, chatRoom, questionId, gameMode);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AnswerQuestion: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }

        public async Task GetNextQuestion(string userName, string chatRoom, int questionIndex, string gameMode)
        {
            try
            {
                Console.WriteLine($"GetNextQuestion called with UserName={userName}, ChatRoom={chatRoom}, QuestionIndex={questionIndex}, GameMode={gameMode}");

                string category = gameMode.Replace("Duel", "");

                int questionsCount = await _dbContext.Questions
                    .Where(q => q.Category == category)
                    .CountAsync();

                if (questionIndex >= questionsCount)
                {
                    Console.WriteLine("No more questions available, ending game.");
                    await EndGame(chatRoom);
                    return;
                }

                var nextQuestion = await _dbContext.Questions
                    .Where(q => q.Category == category)
                    .OrderBy(q => q.QuestionId)
                    .Skip(questionIndex)
                    .FirstOrDefaultAsync();

                if (nextQuestion == null)
                {
                    Console.WriteLine("Next question not found, ending game.");
                    await EndGame(chatRoom);
                    return;
                }

                // Сохраняем текущий индекс вопроса в состоянии игрока
                var playerState = await GetPlayerState(userName);
                if (playerState != null)
                {
                    playerState.CurrentQuestionIndex = nextQuestion.QuestionId;
                    await SavePlayerState(userName, playerState);
                }

                var answers = new List<string>
        {
            nextQuestion.Answer1,
            nextQuestion.Answer2,
            nextQuestion.Answer3,
            nextQuestion.Answer4
        };

                var imageUrl = _imageService.DecodeImageAsync(nextQuestion.ImageData);

                await Clients.Client(Context.ConnectionId).ReceiveQuestion(
                    questionIndex,
                    nextQuestion.QuestionText,
                    imageUrl,
                    nextQuestion.QuestionExplanation,
                    answers);

                Console.WriteLine($"Question sent: {nextQuestion.QuestionId} - {nextQuestion.QuestionText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetNextQuestion: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }



        public async Task EndGame(string chatRoom)
        {
            try
            {
            // Проверяем, активна ли игра для данной комнаты
            if (!Rooms.ContainsKey(chatRoom) || !Rooms[chatRoom].IsGameActive)
            {
                Console.WriteLine($"EndGame already processed or game not active for chatRoom {chatRoom}");
                return;
            }

            var duel = await GetOrCreateDuel(chatRoom);
            var results = new Dictionary<string, int>();

            if (duel != null)
            {
                if (!string.IsNullOrEmpty(duel.Player1))
                {
                    var player1State = await GetPlayerState(duel.Player1);
                    if (player1State != null)
                    {
                        results[duel.Player1] = player1State.Score;
                    }
                }

                if (!string.IsNullOrEmpty(duel.Player2))
                {
                    var player2State = await GetPlayerState(duel.Player2);
                    if (player2State != null)
                    {
                        results[duel.Player2] = player2State.Score;
                    }
                }

                // Log the results before sending
                Console.WriteLine("Results before sending to client:");
                foreach (var result in results)
                {
                    Console.WriteLine($"Player: {result.Key}, Score: {result.Value}");
                }

                await Clients.Group(chatRoom).EndGame(results);

                // Set IsGameActive to false so EndGame cannot be called again
                Rooms[chatRoom].IsGameActive = false;

                // Clear the duel state
                await ClearDuel(chatRoom);

                // Clear the players' states
                if (!string.IsNullOrEmpty(duel.Player1))
                {
                    await ClearPlayerState(duel.Player1);
                }
                if (!string.IsNullOrEmpty(duel.Player2))
                {
                    await ClearPlayerState(duel.Player2);
                }
            }
            else
            {
                Console.WriteLine($"Error: Duel not found for chatRoom {chatRoom}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in EndGame: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            throw;
        }
    }

        private Task ClearDuel(string chatRoom)
        {
            _duelCache.Remove(chatRoom);
            return Task.CompletedTask;
        }

        private Task ClearPlayerState(string userName)
        {
            _playerStateCache.Remove(userName);
            return Task.CompletedTask;
        }

        private Task<PlayerState> GetPlayerState(string userName)
        {
            _playerStateCache.TryGetValue(userName, out var playerState);
            return Task.FromResult(playerState);
        }

        private Task SavePlayerState(string userName, PlayerState playerState)
        {
            _playerStateCache[userName] = playerState;
            return Task.CompletedTask;
        }

        private Task<Duel> GetOrCreateDuel(string chatRoom)
        {
            if (_duelCache.TryGetValue(chatRoom, out var duel))
            {
                return Task.FromResult(duel);
            }
            duel = new Duel(string.Empty, string.Empty, chatRoom, new Dictionary<string, int>());
            _duelCache[chatRoom] = duel;
            return Task.FromResult(duel);
        }

        private Task SaveDuel(string chatRoom, Duel duel)
        {
            _duelCache[chatRoom] = duel;
            return Task.CompletedTask;
        }

        private class PlayerState
        {
            public int CurrentQuestionIndex { get; set; } = 0;
            public int Score { get; set; } = 0;
        }

        public class GameRoom
        {
            public string RoomName { get; set; }
            public List<string> Players { get; set; }
            public bool IsGameActive { get; set; }

            public GameRoom(string roomName)
            {
                RoomName = roomName;
                Players = new List<string>();
                IsGameActive = false; // Изначально игра не активна
            }

            public void StartGame()
            {
                IsGameActive = true;
            }
        }


        public record Duel(string Player1, string Player2, string ChatRoom, Dictionary<string, int> Scores);
    }
}
