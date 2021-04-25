using Microsoft.Azure.Cosmos.Table;

namespace MyHealth.FileValidator.Activity.Models
{
    public class ActivityFileEntity : TableEntity
    {
        public ActivityFileEntity()
        {

        }

        public ActivityFileEntity(string fileName)
        {
            PartitionKey = "Activity";
            RowKey = fileName;
        }
    }
}
