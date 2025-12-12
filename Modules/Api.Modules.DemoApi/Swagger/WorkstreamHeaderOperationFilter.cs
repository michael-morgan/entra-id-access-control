using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Modules.DemoApi.Swagger;

/// <summary>
/// Adds X-Workstream-Id header parameter to all Swagger operations.
/// This header is required by the Access Control Framework for proper authorization scoping.
/// </summary>
public class WorkstreamHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= new List<OpenApiParameter>();

        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Workstream-Id",
            In = ParameterLocation.Header,
            Required = false,
            Description = "Workstream identifier for authorization scope (e.g., 'loans', 'claims', 'documents'). Defaults to 'platform' if not provided.",
            Schema = new OpenApiSchema
            {
                Type = "string",
                Default = new OpenApiString("loans"),
                Enum = new List<IOpenApiAny>
                {
                    new OpenApiString("platform"),
                    new OpenApiString("loans"),
                    new OpenApiString("claims"),
                    new OpenApiString("documents")
                }
            }
        });
    }
}
