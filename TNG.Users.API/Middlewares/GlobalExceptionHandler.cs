using System.Net;
using Ardalis.Result;
using TNG.Users.API.Common.Exceptions;
using static System.String;

namespace TNG.Users.API.Middlewares;

public class GlobalExceptionHandler(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await next(httpContext);
        }
        catch (ValidationException ex)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsJsonAsync(
                Result.Invalid(ex.Errors.Select(e => new ValidationError
                    { Identifier = e.Key, ErrorMessage = Join("; ", e.Value) }).ToList()));
        }
        catch (Exception ex)
        {
            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(Result.Error("Internal Server Error"));
        }
    }
}