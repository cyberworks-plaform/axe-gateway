using Ce.Gateway.Api.Entities;
using Ce.Gateway.Api.Models;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories.Interface
{
    public interface ILogRepository
    {
        Task<PaginatedResult<RequestLogEntry>> GetLogsAsync(LogFilter filter, int page, int pageSize);
    }
}
