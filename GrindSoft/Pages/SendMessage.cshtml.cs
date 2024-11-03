using GrindSoft.Models;
using GrindSoft.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace GrindSoft.Pages
{
    public class SendMessageModel : PageModel
    {
        private readonly SessionManager _sessionManager;
        private readonly AppDbContext _dbContext;

        public SendMessageModel(SessionManager sessionManager, AppDbContext dbContext)
        {
            _sessionManager = sessionManager;
            _dbContext = dbContext; 
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

        [BindProperty]
        public int MessageCount { get; set; }

        [BindProperty]
        public int DelayBetweenMessages { get; set; }

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
                    Status = "In Progress",
                    MessageCount = MessageCount,
                    DelayBetweenMessages = DelayBetweenMessages
                };

            _sessionManager.AddSession(session);

            Response = "Session started and is in progress.";
            return Page();
        }
    }
}
