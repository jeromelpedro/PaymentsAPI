using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

namespace Payments.Api.Configurations
{
	public static class AuthConfig
	{
		public static void AddAuthConfiguration(this IServiceCollection services, IConfiguration configuration)
		{
			services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
				.AddJwtBearer(options =>
				{
					var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!);
					options.TokenValidationParameters = new TokenValidationParameters
					{
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidateLifetime = true,
						ValidateIssuerSigningKey = true,
						ValidIssuer = configuration["Jwt:Issuer"],
						ValidAudience = configuration["Jwt:Audience"],
						IssuerSigningKey = new SymmetricSecurityKey(key)
					};

					options.Events = new JwtBearerEvents
					{
						OnChallenge = async context =>
						{
							context.HandleResponse();
							context.Response.StatusCode = StatusCodes.Status401Unauthorized;
							context.Response.ContentType = "application/json";

							var result = JsonSerializer.Serialize(new
							{
								status = 401,
								error = "Não autorizado",
								message = "Token JWT ausente ou inválido."
							});

							await context.Response.WriteAsync(result);
						},
						OnForbidden = async context =>
						{
							context.Response.StatusCode = StatusCodes.Status403Forbidden;
							context.Response.ContentType = "application/json";

							var result = JsonSerializer.Serialize(new
							{
								status = 403,
								error = "Acesso negado",
								message = "Você não tem permissão para acessar este recurso."
							});

							await context.Response.WriteAsync(result);
						}
					};
				});

			services.AddAuthorization(options =>
			{
				options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
				options.AddPolicy("Leitura", policy => policy.RequireRole("Leitura", "Admin"));
			});
		}
	}
}
