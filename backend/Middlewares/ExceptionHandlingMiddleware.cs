namespace backend.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                LogException(ex, "Unexpected error", LogLevel.Error);
                context.Response.StatusCode = StatusCodes.Status200OK;
            }
        }
        private string GetFullMethodName(Exception ex)
        {
            if (ex.TargetSite == null)
                return "<unknown>";

            var className = ex.TargetSite.DeclaringType?.Name ?? "<unknown>";
            var methodName = ex.TargetSite.Name;
            var parameters = ex.TargetSite.GetParameters();
            var paramList = parameters != null
                ? string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name.ToLower()} {p.Name}"))
                : string.Empty;

            return $"{className}.{methodName}({paramList})";
        }

        private void LogException(Exception ex, string level, LogLevel logLevel = LogLevel.Warning)
        {
            var fullMethodName = GetFullMethodName(ex);
            var details = ex.Message;

            if (logLevel == LogLevel.Error)
                _logger.LogError(ex, "[{Level}] in [{Method}]. Details: {Details}", level, fullMethodName, details);
            else
                _logger.LogWarning(ex, "[{Level}] in [{Method}]. Details: {Details}", level, fullMethodName, details);

            _logger.LogDebug(ex, "StackTrace for {Method}: {StackTrace}", fullMethodName, ex.StackTrace);
        }
    }
}
