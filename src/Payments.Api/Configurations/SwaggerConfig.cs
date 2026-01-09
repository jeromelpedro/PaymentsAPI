using Microsoft.OpenApi.Models;

namespace Payments.Api.Configurations
{
    public static class SwaggerConfig
    {
        public static void AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                // Informações da API
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "FIAP Cloud Games - Tech Challenge - Fase 2 - Payments API",
                    Version = "v1",
                    Description = "API REST em .NET 9 para gerenciar jogos e biblioteca.",
                });

                //JWT
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "Insira o token JWT desta forma: {seu token}",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                var securityRequirement = new OpenApiSecurityRequirement
                {
                    { securityScheme, new[] { "Bearer" } }
                };

                c.AddSecurityRequirement(securityRequirement);
                //c.OperationFilter<AuthorizeCheckOperationFilter>();
            });
        }

        public static void UseSwaggerConfiguration(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
                c.DocumentTitle = "Documentação da API";
                c.RoutePrefix = string.Empty;
            });
        }
    }
}
