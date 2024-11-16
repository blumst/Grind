using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;


namespace GrindSoft.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public string ErrorMessage { get; set; }

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public string Password { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            var expectedPasswordHash = _configuration["AppSettings:PasswordHash"]?.Trim();
            var salt = _configuration["AppSettings:Salt"]?.Trim();

            if (string.IsNullOrEmpty(expectedPasswordHash) || string.IsNullOrEmpty(salt))
            {
                ErrorMessage = "Password is not configured.";
                return Page();
            }

            string enteredPasswordHash = ComputeSha512Hash(Password + salt);

            if (enteredPasswordHash == expectedPasswordHash)
            {
                HttpContext.Session.SetString("Authenticated", "true");
                return Redirect("/SendMessage");
            }
            else
            {
                ErrorMessage = "Invalid password.";
                return Page();
            }
        }

        private static string ComputeSha512Hash(string rawData)
        {
            byte[] bytes = SHA512.HashData(Encoding.UTF8.GetBytes(rawData));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}