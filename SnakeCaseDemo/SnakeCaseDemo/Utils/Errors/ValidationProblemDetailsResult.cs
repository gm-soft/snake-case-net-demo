using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SnakeCaseDemo.Utils.Json;

namespace SnakeCaseDemo.Utils.Errors
{
    public class ValidationProblemDetailsResult : IActionResult
    {
        public async Task ExecuteResultAsync(ActionContext context)
        {
            var modelStateEntries = context.ModelState
                .Where(e => e.Value.Errors.Count > 0)
                .ToArray();

            var errors = new List<ValidationError>();

            if (modelStateEntries.Any())
            {
                foreach (var (key, value) in modelStateEntries)
                {
                    errors.AddRange(value.Errors
                        .Select(modelStateError => new ValidationError(
                            name: key.ToSnakeCase(),
                            description: modelStateError.ErrorMessage)));
                }
            }

            await new JsonErrorResponse<ValidationProblemDetails>(
                context: context.HttpContext,
                error: new ValidationProblemDetails(errors),
                statusCode: ValidationProblemDetails.ValidationStatusCode).WriteAsync();
        }
    }
}