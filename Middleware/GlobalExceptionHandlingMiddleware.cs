using System.Net;
using System.Text.Json;
using AuthSystemApi.Exceptions;

namespace AuthSystemApi.Middleware
{
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred. Request: {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ApiErrorResponse();

            switch (exception)
            {
                case AuthSystemApi.Exceptions.ValidationException validationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Details = validationEx.Message;
                    break;

                case AuthSystemApi.Exceptions.BusinessLogicException businessEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = businessEx.Message;
                    break;

                case AuthSystemApi.Exceptions.DuplicateResourceException duplicateEx:
                    response.StatusCode = (int)HttpStatusCode.Conflict;
                    response.Message = duplicateEx.Message;
                    break;

                case AuthSystemApi.Exceptions.DatabaseException dbEx:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "Database operation failed";
                    response.Details = "Please try again later";
                    break;

                case AuthSystemApi.Exceptions.EmailException emailEx:
                    response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    response.Message = "Email service unavailable";
                    response.Details = "Notification may be delayed";
                    break;

                case System.ComponentModel.DataAnnotations.ValidationException systemValidationEx:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Validation failed";
                    response.Details = systemValidationEx.Message;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response.Message = "Unauthorized access";
                    break;

                case ArgumentNullException:
                case ArgumentException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = "Invalid request parameters";
                    response.Details = exception.Message;
                    break;

                case InvalidOperationException:
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    response.Message = exception.Message;
                    break;

                case KeyNotFoundException:
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    response.Message = "Resource not found";
                    break;

                case TimeoutException:
                    response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response.Message = "Request timeout";
                    break;

                default:
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    response.Message = "An internal server error occurred";
                    response.Details = "Please contact support if the problem persists";
                    break;
            }

            context.Response.StatusCode = response.StatusCode;

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    public class ApiErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
    }
}