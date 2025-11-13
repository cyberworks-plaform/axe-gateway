namespace Ce.Gateway.Api.Models.Common
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public ErrorResponse Error { get; set; }

        public static ApiResponse<T> SuccessResult(T data, string message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static ApiResponse<T> ErrorResult(string message, string details = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Error = new ErrorResponse
                {
                    Message = message,
                    Details = details
                }
            };
        }
    }
}
