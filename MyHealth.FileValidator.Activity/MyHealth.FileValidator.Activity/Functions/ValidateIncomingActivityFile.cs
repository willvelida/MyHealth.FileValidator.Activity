using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyHealth.Common;
using MyHealth.FileValidator.Activity.Models;
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
        private readonly IServiceBusHelpers _serviceBusHelpers;
        private readonly ITableHelpers _tableHelpers;

        public ValidateIncomingActivityFile(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            IActivityRecordParser activityRecordParser,
            IServiceBusHelpers serviceBusHelpers,
            ITableHelpers tableHelpers)
        {
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _activityRecordParser = activityRecordParser;
            _serviceBusHelpers = serviceBusHelpers;
            _tableHelpers = tableHelpers;
        }

        [FunctionName(nameof(ValidateIncomingActivityFile))]
        public async Task Run([BlobTrigger("myhealthfiles/activity_{name}", Connection = "BlobStorageConnectionString")] Stream myBlob, string name, ILogger logger)
        {
            logger.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                name = "activity_" + name;
                ActivityFileEntity activityFileEntity = new ActivityFileEntity(name);

                bool isDuplicate = await _tableHelpers.IsDuplicateAsync<ActivityFileEntity>(activityFileEntity.PartitionKey, activityFileEntity.RowKey);

                if (isDuplicate == true)
                {
                    logger.LogInformation($"Duplicate file {activityFileEntity.RowKey} discarded. Deleting file from Blob Storage Container");
                    await _azureBlobHelpers.DeleteBlobAsync(name);
                    return;
                }
                else
                {
                    logger.LogInformation($"Processing new file: {name}");
                    using (var inputStream = await _azureBlobHelpers.DownloadBlobAsStreamAsync(name))
                    {
                        await _activityRecordParser.ParseActivityStream(inputStream);
                    }
                    logger.LogInformation($"{name} file processed.");

                    logger.LogInformation("Insert file into duplicate table");
                    await _tableHelpers.InsertEntityAsync(activityFileEntity);
                    logger.LogInformation($"File {activityFileEntity.RowKey} inserted into table storage");

                    logger.LogInformation($"Deleting {name} from Blob Storage");
                    await _azureBlobHelpers.DeleteBlobAsync(name);
                    logger.LogInformation($"File {name} has been deleted from Blob Storage");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Exception thrown in {nameof(ValidateIncomingActivityFile)}. Exception: {ex}");
                await _serviceBusHelpers.SendMessageToQueue(_configuration["ExceptionQueue"], ex);
                throw ex;
            }
        }
    }
}
