using GrindSoft.Interface;
using GrindSoft.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GrindSoft.Pages
{
    public class SendMessageModel : PageModel
    {
        private readonly IDiscordMessageClient _discordMessageClient;
        private readonly IChatGPTClient _chatGPTClient;
        private readonly DiscordService _discordService;

        public SendMessageModel(IDiscordMessageClient discordMessageClient, IChatGPTClient chatGPTClient)
        {
            _discordMessageClient = discordMessageClient;
            _chatGPTClient = chatGPTClient;
            _discordService = new DiscordService(discordMessageClient, chatGPTClient);
        }

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
                _discordService.UpdateData(AccessToken, ChannelId, ServerId, UserAgent);
                string gptResponse = await _chatGPTClient.SendMessageAsync(Prompt);
                await _discordMessageClient.SendMessageAsync(AccessToken, UserAgent, ChannelId, gptResponse, ServerId);
                Response = "Message successfully sent.";

                await _discordService.FetchUserIdAsync();
            }
            catch (Exception ex)
            {
                Response = $"Failed to send message: {ex.Message}";
            }

            _discordService.StartMonitoring();
            return Page();
        }
    }
}