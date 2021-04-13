using System;
using System.Collections.Generic;
using System.Text;

namespace MyHealth.FileValidator.Activity.Helpers
{
    public class FunctionOptions
    {
        public string ServiceBusConnectionSetting { get; set; }
        public string BlobStorageConnectionSetting { get; set; }
        public string MyHealthContainerSetting { get; set; }
        public string ActivityTopicSetting { get; set; }
        public string ExceptionTopicSetting { get; set; }
    }
}
