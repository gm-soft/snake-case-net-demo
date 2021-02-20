# How to accept and return snake case formatted JSON in ASP Web API

I was asked to change my ASP Web API application data processing format.  I had to move all the JSON data format from `camelCase` to `snake_case`.

![](/images/snake_vs_camel.jpg)


The standard way to accept and return data in ASP.NET world is camel case. The reason for it was fact that we had to start developing React application as a SPA of our system. I thought that nothing will be a trouble, but I had met some issues. Now, I want to share with you my solution of how to make your ASP Web API and JSON in snake_case be the best friends.

I have implemented in and published a simple template application. A link to the GitHub repository you can find at the end of the article. All the samples will be written for ASP.NET Core built with .net5.

## Changing request and response JSON formats

All we need is to change the property naming policy. The standard one is the Camel Case. Changing it is not a difficult task. You should just create a couple of classes and add some settings to your `Startup.cs` class.

First, you should create methods to convert property names to the Snake Case. We will use Newtonsoft.Json library feature for the task:

```csharp
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Utils.Helpers;

namespace YourNamespace
{
    public static class JsonSerializationExtensions
    {
        private static readonly SnakeCaseNamingStrategy _snakeCaseNamingStrategy
            = new SnakeCaseNamingStrategy();

        private static readonly JsonSerializerSettings _snakeCaseSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = _snakeCaseNamingStrategy
            }
        };

        public static string ToSnakeCase<T>(this T instance)
        {
            if (instance == null)
			  {
			       throw new ArgumentNullException(paramName: nameof(instance));
			   }

            return JsonConvert.SerializeObject(instance, _snakeCaseSettings);
        }

        public static string ToSnakeCase(this string @string)
        {
            if (@string == null)
			  {
			       throw new ArgumentNullException(paramName: nameof(@string));
			   }

            return _snakeCaseNamingStrategy.GetPropertyName(@string, false);
        }
    }
}
```

Here we have a couple of useful overloaded methods: the first one accepts a model to serialize and the second one accepts a string value to convert. We use library class `SnakeCaseNamingStrategy` for naming policy settings. 

Then, we should create a class of NamingPolicy for our Web API application. Let's create a class `SnakeCaseNamingPolicy`:

```csharp
using System.Text.Json;
using Utils.Serialization;

namespace YourNamespace
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => name.ToSnakeCase();
    }
}
```

Here we use the method `ToSnakeCase()` that we have created in the code above. We use the SnakeCaseNamingPolicy instance in the `Startup.cs` file in the `ConfigureServices` method:

```csharp
public class Startup
{
  public void ConfigureServices(IServiceCollection services)
  {
    // ...
    services
        .AddMvc()
        .AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        });
    // ...
  }
}
```

Now our Web API works with the Snake Case: requests and responses are being transformed into JSON with the snake case format.

![](/images/sc_data.png)

But now we have one minor issue…

![](/images/cc_error.png)

The image above represents a validation error. The error’s output format is a mix of Camel Case for keys and the Pascal Case for property names. The behavior of output format was not changed even we have applied a custom name policy.

So, let’s fix the issue.

## Changing validation output JSON format

To change the validation output, we should replace a standard state response factory with our custom one. First, we start from the error class that will form our response:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace YourNamespace
{
    public class ValidationProblemDetails : ProblemDetails
    {
        // 400 status ccode is usually used for input validation errors
        public const int ValidationStatusCode = (int)HttpStatusCode.BadRequest;

        public ValidationProblemDetails(ICollection<ValidationError> validationErrors)
        {
            ValidationErrors = validationErrors;
            Status = ValidationStatusCode;
            Title = "Request Validation Error";
        }

        public ICollection<ValidationError> ValidationErrors { get; }

        public string RequestId => Guid.NewGuid().ToString();
    }
}
```

The class accepts a list of validation errors to show them in the response. The class inherits from standard `ProblemDetails` class from `Microsoft.AspNetCore.Mvc` package. The `RequestId` property makes it simpler to find the log record in the log view UI system. 

Then, you should replace a standard `InvalidModelStateResponseFactory` with our custom one. Here is a replacement class:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Utils.Serialization;

namespace YourNamespace
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
```

And some setting in `Startup.cs` should be placed:

```csharp
public class Startup
{
   // ...
  public void ConfigureServices(IServiceCollection services)
  {
    // ...
    services
        .Configure<ApiBehaviorOptions>(x =>
        {
            x.InvalidModelStateResponseFactory = ctx => new ValidationProblemDetailsResult();
        });
    // ...
  }
}
```

Now your validation error output looks like this:

![](/images/sc_error.png)

Now, our ASP.NET Core application accepts and returns JSON with the Snake Case format, and validation error output was changed too.  Here is a [GitHub repository](https://github.com/maximgorbatyuk/snake-case-net-demo) where you can find the implemented solution.
