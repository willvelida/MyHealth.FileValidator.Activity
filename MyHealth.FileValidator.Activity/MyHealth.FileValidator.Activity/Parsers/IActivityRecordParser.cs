using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace MyHealth.FileValidator.Activity.Parsers
{
    public interface IActivityRecordParser
    {
        Task ParseActivityStream(Stream inputStream);
    }
}
