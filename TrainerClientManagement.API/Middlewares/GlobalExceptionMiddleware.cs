using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TrainerClientManagement.API.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private static readonly JsonSerializerOptions ProblemDetailsJsonOptions =
            new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ein unerwarteter Serverfehler ist aufgetreten.");

                if (context.Response.HasStarted)
                {
                    throw;
                }

                await HandleExceptionAsync(context);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Serverfehler",
                Detail = "Ein unerwarteter Fehler ist aufgetreten. Bitte versuchen Sie es später erneut.",
                Instance = context.Request.Path
            };
            problemDetails.Extensions["traceId"] = context.TraceIdentifier;

            await JsonSerializer.SerializeAsync(
                context.Response.Body,
                problemDetails,
                ProblemDetailsJsonOptions,
                cancellationToken: context.RequestAborted);
        }
    }
}
