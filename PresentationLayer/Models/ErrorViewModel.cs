namespace PresentationLayer.Models
{
    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public string? ErrorMessage { get; set; }  // Добавлено для хранения текста ошибки

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public bool ShowErrorMessage => !string.IsNullOrEmpty(ErrorMessage);  // Проверка на наличие сообщения об ошибке
    }
}
