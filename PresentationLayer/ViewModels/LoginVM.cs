using Newtonsoft.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Введите корректный адрес электронной почты")]
        [DataType(DataType.EmailAddress, ErrorMessage = "Введите корректный адрес электронной почты")]
        public string? Email { get; set; }

        [Required(ErrorMessage ="Введите пароль")]
        [DataType(DataType.Password)]
    
        public string? Password { get; set; }

        [Display(Name ="Запомнить меня")]
        public bool RememberMe { get; set; }
    }
}
