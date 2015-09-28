using System.ComponentModel.DataAnnotations;

namespace NuGet.Gallery.Staging.Web.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; } 
    }
}