using Ce.Gateway.Api.Models;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services
{
    public interface IMonitoringService
    {
        Task<PaginatedResult<LogDto>> GetLogsAsync(LogFilter filter, int page, int pageSize);
    }
}
