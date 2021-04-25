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
        private readonly ITableHelpers _tableHelpers;

        public ValidateIncomingActivityFile(
            IConfiguration configuration,
            IAzureBlobHelpers azureBlobHelpers,
            IActivityRecordParser activityRecordParser,
            ITableHelpers tableHelpers)
        {
            _configuration = configuration;
            _azureBlobHelpers = azureBlobHelpers;
            _activityRecordParser = activityRecordParser;
            _tableHelpers = tableHelpers;
        }

        [FunctionName(nameof(ValidateIncomingActivityFile))]
        public async Task Run([BlobTrigger("myhealthfiles/activity_{name}", Connection = "BlobStorageConnectionString")] Stream myBlob, string name, ILogger logger)
        {
            logger.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            try
            {
                ActivityFileEntity activityFileEntity = new ActivityFileEntity(name);

                bool isDuplicate = await _tableHelpers.IsDuplicateAsync<ActivityFileEntity>(activityFileEntity.PartitionKey, activityFileEntity.RowKey);

                if (isDuplicate == true)
                {
                    logger.LogInformation($"Duplicate file {activityFileEntity.RowKey} discarded");
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
