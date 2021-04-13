using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyHealth.Common;
using MyHealth.FileValidator.Activity;
using MyHealth.FileValidator.Activity.Helpers;
using System.IO;

[assembly: FunctionsStartup(typeof(Startup))]
namespace MyHealth.FileValidator.Activity
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            //builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddLogging();

            builder.Services.AddOptions<FunctionOptions>().Configure<IConfiguration>((settings, configuration) =>
            {
                configuration.GetSection("FunctionOptions").Bind(settings);
            });

            builder.Services.AddSingleton(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new ServiceBusHelpers(config.GetConnectionString("ServiceBusConnectionString"));
            });

            builder.Services.AddSingleton(sp =>
            {
                IConfiguration config = sp.GetService<IConfiguration>();
                return new AzureBlobHelpers(config.GetConnectionString("BlobStorageConnectionString"), config.GetConnectionString("MyHealthContainer"));
            });
        }
    }
}
