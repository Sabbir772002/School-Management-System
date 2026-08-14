namespace AssignmentBackend.Common
{
    public enum OperationStatus
    {
        Success,
        NotFound,
        BadRequest,
        Forbidden,
        Unauthorized
    }

    public class OperationResult<T>
    {
        public OperationStatus Status { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }

        public bool IsSuccess => Status == OperationStatus.Success;

        public static OperationResult<T> Success(T data) => new OperationResult<T>
        {
            Status = OperationStatus.Success,
            Data = data
        };

        public static OperationResult<T> NotFound(string message = "Resource not found") => new OperationResult<T>
        {
            Status = OperationStatus.NotFound,
            ErrorMessage = message
        };

        public static OperationResult<T> BadRequest(string message) => new OperationResult<T>
        {
            Status = OperationStatus.BadRequest,
            ErrorMessage = message
        };

        public static OperationResult<T> Forbidden(string message = "Access forbidden") => new OperationResult<T>
        {
            Status = OperationStatus.Forbidden,
            ErrorMessage = message
        };

        public static OperationResult<T> Unauthorized(string message = "Unauthorized") => new OperationResult<T>
        {
            Status = OperationStatus.Unauthorized,
            ErrorMessage = message
        };
    }

    public class OperationResult : OperationResult<object>
    {
        public static OperationResult Success() => new OperationResult
        {
            Status = OperationStatus.Success
        };

        public static new OperationResult NotFound(string message = "Resource not found") => new OperationResult
        {
            Status = OperationStatus.NotFound,
            ErrorMessage = message
        };

        public static new OperationResult BadRequest(string message) => new OperationResult
        {
            Status = OperationStatus.BadRequest,
            ErrorMessage = message
        };

        public static new OperationResult Forbidden(string message = "Access forbidden") => new OperationResult
        {
            Status = OperationStatus.Forbidden,
            ErrorMessage = message
        };
    }
}
