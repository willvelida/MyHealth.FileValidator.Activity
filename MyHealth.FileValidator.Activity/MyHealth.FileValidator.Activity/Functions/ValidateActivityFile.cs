// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}
using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using MyHealth.Common;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.IO;
using CsvHelper;
using System.Globalization;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Activity.Functions
{
    public class ValidateActivityFile
    {
        private readonly ILogger<ValidateActivityFile> _logger;
        private readonly IConfiguration _configuration;
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly IAzureBlobHelpers _azureBlobHelpers;

        public ValidateActivityFile(
            ILogger<ValidateActivityFile> logger,
            IConfiguration configuration,
            IServiceBusHelpers serviceBusHelpers,
            IAzureBlobHelpers azureBlobHelpers)
        {
            _logger = logger;
            _configuration = configuration;
            _serviceBusHelpers = serviceBusHelpers;
            _azureBlobHelpers = azureBlobHelpers;
        }

        [FunctionName(nameof(ValidateActivityFile))]
        public async Task Run([EventGridTrigger]EventGridEvent eventGridEvent)
        {
            try
            {
                // Get the incoming data from event grid
                var eventData = JObject.Parse(eventGridEvent.Data.ToString());
                var fileUrlToken = eventData["url"];

                if (fileUrlToken == null)
                {
                    throw new ApplicationException("Activity File Url is missing from the incoming event");
                }

                string fileUrl = fileUrlToken.ToString();
                var receivedActivityBlobName = "activity/" + Path.GetFileName(fileUrl);

                // Get the Blob URL
                using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(receivedActivityBlobName))
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
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {               
                _logger.LogError($"Exception thrown in {nameof(ValidateActivityFile)}. Exception: {ex.Message}");
                await _serviceBusHelpers.SendMessageToTopic(_configuration["ExceptionTopicName"], ex);
            }
        }
    }
}
