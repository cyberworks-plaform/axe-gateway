
using System.Threading.Tasks;
using Ce.Gateway.Api.Entities;

namespace Ce.Gateway.Api.Services
{
    public interface ILogWriter
    {
        Task WriteLogAsync(OcrGatewayLogEntry logEntry);
    }
}
