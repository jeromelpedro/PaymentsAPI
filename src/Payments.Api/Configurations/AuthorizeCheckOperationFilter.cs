using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Payments.Api.Configurations
{
	public class AuthorizeCheckOperationFilter : IOperationFilter
	{
		public void Apply(OpenApiOperation operation, OperationFilterContext context)
		{
			// Verifica se o endpoint tem algum requisito de autorização
			var hasAuth = context.ApiDescription.ActionDescriptor
				.EndpointMetadata.OfType<AuthorizeAttribute>().Any()
				|| context.ApiDescription.ActionDescriptor
				.EndpointMetadata.OfType<IAuthorizeData>().Any();

			if (!hasAuth) return;

			operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Não autorizado" });
			operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Proibido" });

			// Busca roles se tiver
			var roles = context.ApiDescription.ActionDescriptor.EndpointMetadata
				.OfType<AuthorizeAttribute>()
				.Select(a => a.Roles)
				.Where(r => !string.IsNullOrEmpty(r));

			if (roles.Any())
			{
				operation.Description += $"<br/><b>Roles necessárias:</b> {string.Join(", ", roles)}";
			}

			// Adiciona ícone de segurança (cadeado) no Swagger
			operation.Security = new List<OpenApiSecurityRequirement>
			{
				new OpenApiSecurityRequirement
				{
					[ new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						}
					] = new List<string>()
				}
			};
		}
	}
}
