using GrindSoft.Models;
using GrindSoft.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GrindSoft.Pages
{
    public class SendMessageModel : PageModel
    {
        private readonly SessionManager _sessionManager;

        public SendMessageModel(SessionManager sessionManager)
        {
            _sessionManager = sessionManager;
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

                var session = new Session
                {
                    AccessToken = AccessToken,
                    UserAgent = UserAgent,
                    ServerId = ServerId ?? "@me",
                    ChannelId = ChannelId,
                    Prompt = Prompt,
                    Status = "In Progress"
                };

            _sessionManager.AddSession(session);

            Response = "Session started and is in progress.";
            return Page();
        }
    }
}
