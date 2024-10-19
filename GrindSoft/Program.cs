
using GrindSoft.BackgroundServices;
using GrindSoft.Interface;
using GrindSoft.Services;
using GrindSoft.Settings;

namespace GrindSoft
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<DiscordSettings>(builder.Configuration.GetSection("DiscordSettings"));
            builder.Services.Configure<ChatGptSettings>(builder.Configuration.GetSection("ChatGptSettings"));

            builder.Services.AddRazorPages();

            builder.Services.AddScoped<IDiscordClient, DiscordClient>();
            builder.Services.AddScoped<IChatGPTClient, ChatGPTClient>();
            builder.Services.AddSingleton<SessionManager>();

            builder.Services.AddHostedService<SessionProcessingService>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
              
            }

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/SendMessage");
                return Task.CompletedTask;
            });

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapRazorPages();

            app.Run();
        }
    }
}
