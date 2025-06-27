using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace ContractHome.Controllers.Filters
{
    public class ExecutionLogFilter(ILogger<ExecutionLogFilter> logger) : IActionFilter
    {
        private readonly ILogger<ExecutionLogFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        private readonly Stopwatch _stopwatch = new();

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();

            var user = context.HttpContext.User.Claims.FirstOrDefault()?.Value ?? "Anonymous";
            var path = context.HttpContext.Request.Path;
            var method = context.HttpContext.Request.Method;
            var elapsedMs = _stopwatch.ElapsedMilliseconds;
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString();
            ip ??= context.HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "Unknown";

            _logger.LogInformation($"[PID: {user}] [IP: {ip}] [Method: {method}] [Path: {path}] Time: {elapsedMs}ms");
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch.Restart();
        }
    }
}
