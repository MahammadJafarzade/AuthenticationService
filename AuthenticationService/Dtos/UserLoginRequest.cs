using System.ComponentModel.DataAnnotations;

namespace AuthenticationService.Dtos
{
    public class UserLoginRequest
    {
        [Required]
        public string Username { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
