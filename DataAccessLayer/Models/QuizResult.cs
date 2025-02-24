using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Models
{
    public class QuizResult
    {
        public int Id { get; set; }

        // Явно указываем тип для внешнего ключа
        public string UserId { get; set; }
        public User User { get; set; } // Навигационное свойство

        public int Score { get; set; }
        public string? Type { get; set; }
        public DateTime DatePlayed { get; set; }
        public int? Place { get; set; } // Для дуэлей
    }
}
