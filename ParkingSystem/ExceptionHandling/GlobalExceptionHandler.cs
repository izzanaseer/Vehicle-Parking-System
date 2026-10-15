using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ParkingSystem.ExceptionHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Unhandled exception on {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        var (status, title) = exception switch
        {
            DbUpdateConcurrencyException
                => (StatusCodes.Status409Conflict, "This record was changed by another request. Please reload and try again."),
                
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                => (StatusCodes.Status409Conflict, "A record with the same unique value already exists."),
            
            _ when IsDeadlock(exception)
                => (StatusCodes.Status409Conflict, "The system was busy with another request. Please try again."),  
            
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        var problem = new ProblemDetails { Status = status, Title = title };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }

    private static bool IsDeadlock(Exception exception)
    {
        for (var e = exception; e is not null; e = e.InnerException)
        {
            if (e is SqlException { Number: 1205 })
            {
                return true;
            }
        }
        return false;
    }
}