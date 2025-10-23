
using System.Threading.Tasks;
using Ce.Gateway.Api.Entities;

namespace Ce.Gateway.Api.Repositories.Interface
{
    public interface ILogWriter
    {
        Task WriteLogAsync(RequestLogEntry logEntry);
    }
}
