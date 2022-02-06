using Microsoft.EntityFrameworkCore;
using SACA.Data;
using System.Net;

namespace SACA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
#if !DEBUG
                    context.Database.Migrate();
#endif
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating or initializing the database");
            }

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    //webBuilder.UseSentry();
                    webBuilder.UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 5800);
                    });

                    webBuilder.UseStartup<Startup>();
                });
        }
    }
}