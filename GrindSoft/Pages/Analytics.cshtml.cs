using GrindSoft.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace GrindSoft.Pages
{
    public class AnalyticsModel(AppDbContext dbContext) : PageModel
    {
        private readonly AppDbContext _dbContext = dbContext;

        public int SessionId { get; set; }

        public void OnGet(int sessionId)
        {
            SessionId = sessionId;
        }

        public async Task<IActionResult> OnGetDataAsync(int sessionId)
        {
            var session = await _dbContext.Sessions.FindAsync(sessionId);

            if (session == null)
                return Content("<div class='alert alert-danger'>Session not found.</div>", "text/html");

            var duration = DateTime.UtcNow - session.StartTime;
            var messagesSent = session.MessagesSentByBot;
            var progress = session.MessageCount > 0 ? (double)messagesSent / session.MessageCount * 100 : 0;
            var status = session.Status;
            var errorMessage = session.ErrorMessage;

            var content = $@"
                <div class='analytics-item'>Program operating time: {duration:hh\:mm\:ss}</div>
                <div class='analytics-item'>Number of sent messages: {messagesSent}/{session.MessageCount}</div>
                <div class='analytics-item'>Session Status: {status}</div>
                <div class='analytics-item'>Progress: {progress:F2}%</div>";

            if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(errorMessage))
            {
                content += $@"
                <div class='alert alert-danger mt-3'>
                <strong>Error:</strong> {errorMessage}
                </div>";
            }

            return Content(content, "text/html");
        }


    }
}
