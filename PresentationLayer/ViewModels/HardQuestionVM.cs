namespace PresentationLayer.ViewModels
{
    public class HardQuestionVM
    {
        public string QuestionText { get; set; }

        // Обязательное поле для всех таблиц
        public string CorrectAnswer { get; set; }
        public string CorrectAnswer2 { get; set; }
        public string QuestionExplanation { get; set; }
        public string Category { get; set; }
        // Загружаемое фото
        public IFormFile ImageFile { get; set; }
    }
}
