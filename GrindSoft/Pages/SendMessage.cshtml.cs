using GrindSoft.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace GrindSoft.Pages
{
    public class SendMessageModel(IDiscordMessageClient discordMessageClient) : PageModel
    {
        private readonly IDiscordMessageClient _discordMessageClient = discordMessageClient;

        [BindProperty]
        public string AccessToken { get; set; }

        [BindProperty]
        public string UserAgent { get; set; }

        [BindProperty]
        public string? ServerId { get; set; }

        [BindProperty]
        public string ChannelId { get; set; } 

        [BindProperty]
        public string Message { get; set; }

        public string? Response { get; set; }

        public void OnGet()
        {
            
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                Response = "Please fill in all required fields.";
                return Page();
            }

            try
            {
                await _discordMessageClient.SendMessageAsync(AccessToken, UserAgent, ChannelId, Message, ServerId);
                Response = "Message successfully sent.";
            }
            catch (Exception ex)
            {
                Response = $"Failed to send message: {ex.Message}";
            }

            return Page();
        }
    }
}