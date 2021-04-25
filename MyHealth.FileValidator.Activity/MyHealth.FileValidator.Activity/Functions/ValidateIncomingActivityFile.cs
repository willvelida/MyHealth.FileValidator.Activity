using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Activity.Functions
{
    public class ValidateIncomingActivityFile
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly IAzureBlobHelpers _azureBlobHelpers;

        public ValidateIncomingActivityFile(
            IConfiguration configuration,
            IServiceBusHelpers serviceBusHelpers,
            IAzureBlobHelpers azureBlobHelpers)
        {
            _configuration = configuration;
            _serviceBusHelpers = serviceBusHelpers;
            _azureBlobHelpers = azureBlobHelpers;
        }

        [FunctionName(nameof(ValidateIncomingActivityFile))]
        public async Task Run([BlobTrigger("myhealthfiles/{name}", Connection = "BlobStorageConnectionString")]Stream myBlob, string name, ILogger logger)
        {
            logger.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(name))
                {
                    inputStream.Seek(0, SeekOrigin.Begin);

                    using (var activityStream = new StreamReader(inputStream))
                    using (var csv = new CsvReader(activityStream, CultureInfo.InvariantCulture))
                    {
                        if (csv.Read())
                        {
                            csv.ReadHeader();
                            while (csv.Read())
                            {
                                var activity = new mdl.Activity
                                {
                                    ActivityDate = csv.GetField("Date"),
                                    CaloriesBurned = int.Parse(csv.GetField("Calories Burned"), NumberStyles.AllowThousands),
                                    Steps = int.Parse(csv.GetField("Steps"), NumberStyles.AllowThousands),
                                    Distance = double.Parse(csv.GetField("Distance")),
                                    Floors = int.Parse(csv.GetField("Floors"), NumberStyles.AllowThousands),
                                    MinutesSedentary = int.Parse(csv.GetField("Minutes Sedentary"), NumberStyles.AllowThousands),
                                    MinutesLightlyActive = int.Parse(csv.GetField("Minutes Lightly Active"), NumberStyles.AllowThousands),
                                    MinutesFairlyActive = int.Parse(csv.GetField("Minutes Fairly Active"), NumberStyles.AllowThousands),
                                    MinutesVeryActive = int.Parse(csv.GetField("Minutes Very Active"), NumberStyles.AllowThousands),
                                    ActivityCalories = int.Parse(csv.GetField("Activity Calories"), NumberStyles.AllowThousands)
                                };
                              
                                await _serviceBusHelpers.SendMessageToTopic(_configuration["ActivityTopic"], activity);
                                logger.LogInformation($"Activity Message for {activity.ActivityDate} has been sent");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception thrown in {nameof(ValidateIncomingActivityFile)}. Exception: {ex}");
                throw ex;
            }
        }
    }
}
