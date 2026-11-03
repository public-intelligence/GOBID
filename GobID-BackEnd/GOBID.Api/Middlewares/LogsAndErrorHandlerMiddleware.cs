using Exceptionless;
using GOBID.Aplication.Exceptions;
using GOBID.Application.Wrappers;
using GOBID.Domain.Entities;
using GOBID.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace  GOBID.Api.Middlewares
{
    public class LogsAndErrorHandlerMiddleware
    {
        const string MessageTemplate = "";
        private readonly RequestDelegate _next;
        private readonly ExceptionlessClient _exceptionlessClient;
        private readonly IServiceScopeFactory _scopeFactory;

        public LogsAndErrorHandlerMiddleware(RequestDelegate next, ExceptionlessClient exceptionlessClient, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _exceptionlessClient = exceptionlessClient;
            _scopeFactory = scopeFactory;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.Request.Headers.TryGetValue("X-Correlation-Id", out var existingId) && !string.IsNullOrWhiteSpace(existingId)
                ? existingId.ToString()
                : Guid.NewGuid().ToString("N");

            context.Items["X-Correlation-Id"] = correlationId;
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey("X-Correlation-Id"))
                    context.Response.Headers["X-Correlation-Id"] = correlationId;
                return Task.CompletedTask;
            });

            var sw = Stopwatch.StartNew();
            try
            {
                await _next(context);
                sw.Stop();

                var statusCode = context.Response?.StatusCode;
                if (statusCode == 401 || statusCode == 403)
                    await LogAuthFailureAsync(context, correlationId, statusCode.Value);

                LogginDefault(context, sw);

            }
            catch (Exception ex)
            {
                var response = context.Response;
                response.ContentType = "application/json";
                string message = (ex != null && ex.Message != null) ? ex.Message : "";
                message += (ex != null && ex.InnerException != null) ? ex.InnerException.GetBaseException().Message : "";

                var responseModel = new Response<string>() { Succeeded = false, Message = message };

                response.StatusCode = (int)HttpStatusCode.InternalServerError;

                switch (ex)
                {
                    case ApiException e:
                        //custom application error
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        break;
                    case ApiValidationException e:
                        //custom application error
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel.Errors = e.Errors;
                        break;
                    case KeyNotFoundException e:
                        //not found error
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        break;
                    default:
                        // unhandled error
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        break;
                }
                sw.Stop();
                LogginError(context, sw, ex!);

                var result = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);

            }
        }

        private async Task LogAuthFailureAsync(HttpContext context, string correlationId, int statusCode)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PrincipalContext>();

                var ip = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
                if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff) && !string.IsNullOrWhiteSpace(xff))
                {
                    var first = xff.ToString().Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrWhiteSpace(first)) ip = first;
                }

                dbContext.SecurityAuditLogs.Add(new SecurityAuditLog
                {
                    EventType = SecurityAuditEventTypes.AuthzFailure,
                    CorrelationId = correlationId,
                    Ip = ip,
                    UserAgent = context.Request.Headers.UserAgent.ToString(),
                    Details = JsonSerializer.Serialize(new
                    {
                        status_code = statusCode,
                        path = context.Request.Path.ToString(),
                        method = context.Request.Method
                    }),
                    Success = false,
                    CreatedAt = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Fail silently — do not break the request pipeline
            }
        }

        private void LogginDefault(HttpContext context, Stopwatch sw)
        {

            var statusCode = context.Response?.StatusCode;
            string LogInfo = string.Format("HTTP {0} {1} responded {2} in {3} ms", context.Request.Method, context.Request.Path, statusCode, sw.Elapsed.TotalMilliseconds);

            _exceptionlessClient.SubmitLog(LogInfo);

        }

        private void LogginError(HttpContext context, Stopwatch sw, Exception ex)
        {

            var statusCode = context.Response?.StatusCode;
            string errorMessage = (ex != null && ex.Message != null) ? ex.Message : "";
            string innerMessage = (ex != null && ex.InnerException != null && ex.InnerException != null) ? ex.InnerException.GetBaseException().Message : "";
            string LogInfo = string.Format("HTTP {0} {1} responded {2} in {3} ms, ex.Message=({4}, {5})", context.Request.Method, context.Request.Path, statusCode, sw.Elapsed.TotalMilliseconds, errorMessage, innerMessage);
            _exceptionlessClient.SubmitException(ex!);
            if (statusCode == (int)HttpStatusCode.InternalServerError) _exceptionlessClient.SubmitLog(LogInfo, Exceptionless.Logging.LogLevel.Error); else _exceptionlessClient.SubmitLog(LogInfo, Exceptionless.Logging.LogLevel.Warn);

        }

    }
}
