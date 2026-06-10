using DocumentPortalIam.Back.Core.Dtos;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DocumentPortalIam.Back.Core.Swagger;

public sealed class SwaggerUsageOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var controller = context.ApiDescription.ActionDescriptor.RouteValues["controller"];
        var action = context.ApiDescription.ActionDescriptor.RouteValues["action"];

        if (controller == "Auth" && action == "Login")
        {
            operation.RequestBody = CreateRequestBody<LoginRequestDto>(
                "Credenciais LDAP. Exemplos: admin/Admin@123, gestor/Gestor@123, aluno/Aluno@123, auditor/Auditor@123.");
        }

        if (controller == "OAuth" && action == "Token")
        {
            operation.RequestBody = CreateRequestBody<ClientCredentialsRequestDto>(
                "Credenciais tecnicas M2M: client_id=storage-client e client_secret=M2M@123.");
        }

        if (controller == "M2M" && action == "Export")
        {
            operation.Parameters ??= new List<IOpenApiParameter>();
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Required = true,
                Description = "Cole aqui: Bearer {access_token} obtido em POST /api/oauth/token.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String
                }
            });
        }

        AddCommonResponses(operation);
    }

    private static OpenApiRequestBody CreateRequestBody<TRequest>(string description)
    {
        var schema = typeof(TRequest) == typeof(LoginRequestDto)
            ? CreateObjectSchema(("userName", "admin"), ("password", "Admin@123"))
            : CreateObjectSchema(("client_id", "storage-client"), ("client_secret", "M2M@123"));

        return new OpenApiRequestBody
        {
            Description = description,
            Required = true,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new()
                {
                    Schema = schema
                },
                ["application/x-www-form-urlencoded"] = new()
                {
                    Schema = schema
                }
            }
        };
    }

    private static OpenApiSchema CreateObjectSchema(params (string Name, string Example)[] properties)
    {
        return new OpenApiSchema
        {
            Type = JsonSchemaType.Object,
            Required = new HashSet<string>(properties.Select(property => property.Name)),
            Properties = properties.ToDictionary<(string Name, string Example), string, IOpenApiSchema>(
                property => property.Name,
                property => new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Description = $"Exemplo: {property.Example}"
                })
        };
    }

    private static void AddCommonResponses(OpenApiOperation operation)
    {
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400", new OpenApiResponse { Description = "Requisicao invalida ou regra de negocio rejeitada." });
        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Nao autenticado ou token M2M invalido." });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Autenticado, mas sem permissao RBAC para esta acao." });
        operation.Responses.TryAdd("404", new OpenApiResponse { Description = "Recurso nao encontrado." });
    }
}
