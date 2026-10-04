using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HomeServices.Api.Errors;

/// <summary>
/// Requests the API can't even read (broken JSON, an unknown enum value, a missing required field)
/// get the same 400 shape as validation errors: <c>code: "validation_failed"</c> and
/// <c>errors: { field: ["value.invalid" | "value.required"] }</c>.
/// </summary>
public static class InvalidRequestProblems
{
    public const string Invalid = "value.invalid";
    public const string Required = "value.required";

    public static IActionResult Create(ActionContext context)
    {
        var problem = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(context.HttpContext, StatusCodes.Status400BadRequest, "Validation failed");
        problem.Extensions["code"] = "validation_failed";
        problem.Extensions["errors"] = Errors(context);

        var result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }

    private static Dictionary<string, string[]> Errors(ActionContext context)
    {
        var modelState = context.ModelState;
        var parameters = context.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var hasJsonErrors = modelState.Keys.Any(k => k.StartsWith('$'));

        return modelState
            .Where(entry => entry.Value!.Errors.Count > 0)

            // Broken JSON also reports the whole body parameter ("request is required"); the JSON error says more.
            .Where(entry => !(hasJsonErrors && parameters.Contains(entry.Key)))
            .GroupBy(entry => FieldName(entry.Key))
            .ToDictionary(g => g.Key, g => g.Select(entry => CodeFor(entry.Key, entry.Value!)).Distinct().ToArray());
    }

    // JSON errors name a path ("$.type", "$.areas[0].cityId"); model validation names a property ("DisplayName").
    private static string FieldName(string key)
    {
        var name = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key.TrimStart('$');
        if (name.Length == 0)
        {
            return "body";
        }

        return string.Join('.', name.Split('.').Select(part => part.Length == 0 ? part : char.ToLowerInvariant(part[0]) + part[1..]));
    }

    private static string CodeFor(string key, ModelStateEntry entry) =>
        key.StartsWith('$') || entry.Errors.Any(e => e.Exception is not null) ? Invalid : Required;
}
