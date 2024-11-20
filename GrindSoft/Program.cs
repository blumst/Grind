
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
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            builder.Configuration.AddEnvironmentVariables();

            builder.Services.Configure<DiscordSettings>(builder.Configuration.GetSection("DiscordSettings"));
            builder.Services.Configure<ChatGptSettings>(builder.Configuration.GetSection("ChatGptSettings"));

            builder.Services.AddRazorPages();

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IDiscordClient, DiscordClient>();
            builder.Services.AddScoped<IChatGPTClient, ChatGPTClient>();
            builder.Services.AddSingleton<SessionManager>();

            builder.Services.AddHostedService<SessionProcessingService>();

            builder.Services.AddDistributedMemoryCache();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                dbContext.Database.EnsureDeleted();
                await dbContext.Database.MigrateAsync();
            }

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

            app.UseSession();

            app.Use(async (context, next) =>
            {
                var config = context.RequestServices.GetRequiredService<IConfiguration>();
                var expectedPassword = config["AppSettings:PasswordHash"];

                if (context.Request.Path.StartsWithSegments("/Login"))
                {
                    await next.Invoke();
                    return;
                }

                if (!context.Session.Keys.Contains("Authenticated"))
                {
                    context.Response.Redirect("/Login");
                    return;
                }

                await next.Invoke();
            });

            app.UseAuthorization();

            app.MapRazorPages();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
