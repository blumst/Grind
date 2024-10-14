using GrindSoft.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace GrindSoft.Pages
{
    public class SendMessageModel(IDiscordMessageClient discordMessageClient, IChatGPTClient chatGPTClient) : PageModel
    {
        private readonly IDiscordMessageClient _discordMessageClient = discordMessageClient;
        private readonly IChatGPTClient _chatGPTClient = chatGPTClient;

        [BindProperty]
        public string AccessToken { get; set; }

        [BindProperty]
        public string UserAgent { get; set; }

        [BindProperty]
        public string? ServerId { get; set; }

        [BindProperty]
        public string ChannelId { get; set; }

        [BindProperty]
        public string Prompt { get; set; }

        //[BindProperty]
        //public string Message { get; set; }

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
                //await _discordMessageClient.SendMessageAsync(AccessToken, UserAgent, ChannelId, Message, ServerId);
                //Response = "Message successfully sent.";

                // ќтримуЇм в≥дпов≥дь от ChatGPT
                string gptResponse = await _chatGPTClient.SendMessageAsync(Prompt);

                //  идаЇмо ≥дпов≥дь в Discord
                await _discordMessageClient.SendMessageAsync(AccessToken, UserAgent, ChannelId, gptResponse, ServerId);
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