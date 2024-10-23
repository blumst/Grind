
using GrindSoft.BackgroundServices;
using GrindSoft.Interface;
using GrindSoft.Models;
using GrindSoft.Services;
using GrindSoft.Settings;
using Microsoft.EntityFrameworkCore;

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

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

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

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.MigrateAsync();
            }

            app.Run();
        }
    }
}
