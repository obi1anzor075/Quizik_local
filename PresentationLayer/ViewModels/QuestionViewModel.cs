namespace PresentationLayer.ViewModels
{
    // Модель для привязки данных из формы
    public class QuestionViewModel
    {
        // Выбранная таблица (например, "EasyQuestion", "DuelQuestion", "HardQuestion")
        public string TableName { get; set; }

        public string QuestionText { get; set; }

        // Обязательное поле для всех таблиц
        public string CorrectAnswer { get; set; }

        // Только для HardQuestion
        public string CorrectAnswer2 { get; set; }

        // Поля для вариантов ответов (предполагается, что нужны для Easy и Duel)
        public string Answer1 { get; set; }
        public string Answer2 { get; set; }
        public string Answer3 { get; set; }
        public string Answer4 { get; set; }

        // Загружаемое фото
        public IFormFile ImageFile { get; set; }
    }
}
