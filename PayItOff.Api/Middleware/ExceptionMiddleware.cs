using FluentValidation;
using PayItOff.Domain.Exceptions;
using System.Net;
using System.Security.Authentication;
using System.Text.Json;

namespace PayItOff.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.ContentType = "application/json";

            if (ex is ValidationException validationEx)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                var errors = validationEx.Errors.Select(e => e.ErrorMessage);
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { Errors = errors }, _jsonOptions));
            }
            else if (ex is PayItOffException domainEx)
            {
                // Mapowanie: Wyjątek -> Kod HTTP
                context.Response.StatusCode = domainEx switch
                {
                    InvalidPasswordException => (int)HttpStatusCode.BadRequest,  // 400
                    UserNotActiveOrVerifiedException => (int)HttpStatusCode.BadRequest,  // 400
                    UserNotFoundException => (int)HttpStatusCode.NotFound,  // 404
                    GroupNotFoundException => (int)HttpStatusCode.NotFound,  // 404
                    FriendInviteNotFoundException => (int)HttpStatusCode.NotFound,  // 404
                    UserAlreadyExistsException => (int)HttpStatusCode.Conflict,  // 409
                    FriendInvitationAlreadyExistsException => (int)HttpStatusCode.Conflict,  //409
                    _ => (int)HttpStatusCode.BadRequest
                };
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = domainEx.Message }, _jsonOptions));
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                await context.Response.WriteAsync(JsonSerializer.Serialize(new { Error = ex.Message }, _jsonOptions));
            }
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}