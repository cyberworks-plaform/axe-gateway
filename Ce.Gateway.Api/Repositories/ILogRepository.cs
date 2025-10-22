using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories
{
    public interface ILogRepository
    {
        Task<PaginatedResult<OcrGatewayLogEntry>> GetLogsAsync(LogFilter filter, int page, int pageSize);
    }
}
