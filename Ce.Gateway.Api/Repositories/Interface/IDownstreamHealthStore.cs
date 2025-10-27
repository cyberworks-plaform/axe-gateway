using System.Collections.Generic;
using Ce.Gateway.Api.Models;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Repositories.Interface
{
    public interface IDownstreamHealthStore
    {
        Task<IEnumerable<DownstreamServiceHealth>> GetAllHealthAsync();
        Task<DownstreamServiceHealth> GetHealthAsync(string host, int port);
        Task UpdateHealthAsync(DownstreamServiceHealth health);
        Task UpdateHealthAsync(IEnumerable<DownstreamServiceHealth> healths);
    }
}
