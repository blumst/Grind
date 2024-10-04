
using GrindSoft.Interface;
using GrindSoft.Services;

namespace GrindSoft
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();

            builder.Services.AddScoped<IDiscordMessageClient, DiscordMessageClient>();

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
