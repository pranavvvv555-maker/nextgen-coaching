using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await _next(ctx);
        }
        catch (BadHttpRequestException ex)
        {
            await WriteProblem(ctx, ex.StatusCode, "Bad Request", "The request contains invalid or missing parameters.");
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status404NotFound, "Not Found", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status400BadRequest, "Bad Request", ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            await WriteProblem(ctx, StatusCodes.Status409Conflict, "Conflict", "A record with these details already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            await WriteProblem(ctx, StatusCodes.Status400BadRequest, "Bad Request", "A referenced record could not be found. Please refresh and try again.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await WriteProblem(ctx, StatusCodes.Status409Conflict, "Conflict",
                "This record was changed by someone else while you were editing. Reload and try again.");
        }
        catch (InvalidOperationException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status409Conflict, "Conflict", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Path}", ctx.Request.Path);
            var detail = _env.IsDevelopment() ? ex.ToString() : "An unexpected error occurred.";
            await WriteProblem(ctx, StatusCodes.Status500InternalServerError, "Server Error", detail);
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";

        var problem = new ProblemResponse
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            TraceId = ctx.TraceIdentifier
        };

        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
