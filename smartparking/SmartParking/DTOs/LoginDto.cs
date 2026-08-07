using System.ComponentModel.DataAnnotations;

namespace SmartParking.DTOs
{
    public class LoginDto
    {
        public string? Email { get; set; }
        public string? Login { get; set; }

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
