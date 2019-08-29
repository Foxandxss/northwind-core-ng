using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using NorthwindModel.Models;
using System;

namespace NorthwindServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Contains("metadata"))
            {
                // Generate metadata and exit
                var optionsBuilder = new DbContextOptionsBuilder<NorthwindCoreContext>().UseInMemoryDatabase("foo", null);
                var dbContext = new NorthwindCoreContext(optionsBuilder.Options);
                var persistenceManager = new NorthwindCorePersistenceManager(dbContext);
                var metadata = persistenceManager.Metadata();
                Console.Out.WriteLine(metadata);
            }
            else
            {
                // Start web server
                CreateWebHostBuilder(args).Build().Run();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
