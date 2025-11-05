using System.Collections.Generic;

namespace Ce.Gateway.Api.Models
{
    public class PaginatedResult<T>
    {
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IEnumerable<T> Data { get; set; }
    }
}
