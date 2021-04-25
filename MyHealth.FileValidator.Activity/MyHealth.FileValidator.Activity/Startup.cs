using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.FileValidator.Activity;
using MyHealth.FileValidator.Activity.Functions;
using MyHealth.FileValidator.Activity.Parsers;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]
namespace MyHealth.FileValidator.Activity
{
    public class Startup : FunctionsStartup
    {
        private static ILogger _logger;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddLogging();
            _logger = new LoggerFactory().CreateLogger(nameof(ValidateIncomingActivityFile));

            builder.Services.AddSingleton<IServiceBusHelpers>(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new ServiceBusHelpers(config["ServiceBusConnectionString"]);
            });

            builder.Services.AddSingleton<IAzureBlobHelpers>(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new AzureBlobHelpers(config["BlobStorageConnectionString"], config["MyHealthContainer"]);
            });

            builder.Services.AddScoped<IActivityRecordParser, ActivityRecordParser>();
        }
    }
}
