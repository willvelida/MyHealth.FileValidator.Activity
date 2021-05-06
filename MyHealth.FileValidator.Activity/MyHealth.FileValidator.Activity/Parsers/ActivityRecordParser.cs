using CsvHelper;
using Microsoft.Extensions.Configuration;
using MyHealth.Common;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using mdl = MyHealth.Common.Models;

namespace MyHealth.FileValidator.Activity.Parsers
{
    public class ActivityRecordParser : IActivityRecordParser
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceBusHelpers _serviceBusHelpers;

        public ActivityRecordParser(
            IConfiguration configuration,
            IServiceBusHelpers serviceBusHelpers)
        {
            _configuration = configuration;
            _serviceBusHelpers = serviceBusHelpers;
        }

        public async Task ParseActivityStream(Stream inputStream)
        {
            try
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
                                ActivityDate = ParseActivityDate(csv.GetField("Date")),
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
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string ParseActivityDate(string activityDate)
        {
            DateTime activityDateToDateTime = DateTime.ParseExact(activityDate, "d/MM/yyyy", null);
            string parsedActivityDate = activityDateToDateTime.ToString("yyyy-MM-dd");
            return parsedActivityDate;
        }
    }
}
