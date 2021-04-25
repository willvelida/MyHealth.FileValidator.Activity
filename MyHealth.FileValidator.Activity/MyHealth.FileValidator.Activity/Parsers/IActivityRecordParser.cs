using System.IO;
using System.Threading.Tasks;

namespace MyHealth.FileValidator.Activity.Parsers
{
    public interface IActivityRecordParser
    {
        Task ParseActivityStream(Stream inputStream);
    }
}
