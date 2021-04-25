using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.FileValidator.Activity.Parsers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MyHealth.FileValidator.Activity.Functions
{
    public class ValidateIncomingActivityFile
    {
        private readonly IConfiguration _configuration;
        private readonly IAzureBlobHelpers _azureBlobHelpers;
        private readonly IActivityRecordParser _activityRecordParser;

        public ValidateIncomingActivityFile(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            IActivityRecordParser activityRecordParser)
        {
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _activityRecordParser = activityRecordParser;
        }

        [FunctionName(nameof(ValidateIncomingActivityFile))]
        public async Task Run([BlobTrigger("myhealthfiles/{name}", Connection = "BlobStorageConnectionString")] Stream myBlob, string name, ILogger logger)
        {
            logger.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(name))
                {
                    await _activityRecordParser.ParseActivityStream(inputStream);
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
