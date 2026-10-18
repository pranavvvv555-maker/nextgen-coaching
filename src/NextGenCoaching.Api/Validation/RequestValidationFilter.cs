using System.ComponentModel.DataAnnotations;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Api.Validation;

/// <summary>Minimal APIs do not automatically run the DTO DataAnnotations.</summary>
public sealed class RequestValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var errors = new List<string>();
        foreach (var argument in context.Arguments)
        {
            if (argument is null || argument.GetType().Namespace != "NextGenCoaching.Shared.Requests") continue;
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(argument, new ValidationContext(argument), results, validateAllProperties: true);
            errors.AddRange(results.Select(r => r.ErrorMessage ?? "Invalid request field."));
        }
        if (errors.Count > 0)
            return Results.BadRequest(ApiResponse<object>.Fail("Please check the submitted information.", errors));
        return await next(context);
    }
}
