using System.ComponentModel.DataAnnotations.Schema;

namespace PresentationLayer.ViewModels
{
    // Модель для привязки данных из формы
    public class QuestionVM
    {
        public string QuestionText { get; set; }

        // Обязательное поле для всех таблиц
        public int CorrectAnswerIndex { get; set; }


        // Поля для вариантов ответов (предполагается, что нужны для Easy и Duel)
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }
        public string QuestionExplanation { get; set; }
        public string Category { get; set; }
        // Загружаемое фото
        public IFormFile ImageFile { get; set; }
    }
}
