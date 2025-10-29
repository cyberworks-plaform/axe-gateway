using Ce.Gateway.Api.Models;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Services.Interface
{
    public interface IRequestLogService
    {
        Task<PaginatedResult<LogDto>> GetLogsAsync(LogFilter filter, int page, int pageSize);
    }
}
