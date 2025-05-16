using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace EHR.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = HttpStatusCode.InternalServerError;
            var message = "An unexpected error occurred.";
            var details = _env.IsDevelopment() ? exception.ToString() : null;

            switch (exception)
            {
                case DbUpdateConcurrencyException:
                    statusCode = HttpStatusCode.Conflict;
                    message = "The record was modified by another user. Please refresh and try again.";
                    break;

                case DbUpdateException dbEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "A database error occurred while processing your request.";
                    if (_env.IsDevelopment())
                    {
                        details = dbEx.InnerException?.Message ?? dbEx.Message;
                    }
                    break;

                case UnauthorizedAccessException:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = "You are not authorized to perform this action.";
                    break;

                case ArgumentException argEx:
                    statusCode = HttpStatusCode.BadRequest;
                    message = argEx.Message;
                    break;

                case InvalidOperationException:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "The operation cannot be performed in the current state.";
                    break;
            }

            _logger.LogError(exception, 
                "An error occurred processing the request. Path: {Path}, Method: {Method}, StatusCode: {StatusCode}",
                context.Request.Path, context.Request.Method, statusCode);

            var response = new
            {
                StatusCode = (int)statusCode,
                Message = message,
                Details = details
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
} 