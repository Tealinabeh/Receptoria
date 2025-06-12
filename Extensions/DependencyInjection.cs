using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Receptoria.API.Data;
using Receptoria.API.GraphQL;
using Receptoria.API.GraphQL.Resolvers;
using Receptoria.API.Models;
using Receptoria.API.Services;

namespace Receptoria.API.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddNpgsql(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionUriString = configuration.GetConnectionString("DefaultConnection");

        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder();

        if (!string.IsNullOrEmpty(connectionUriString))
        {
            var connectionUri = new Uri(connectionUriString);
            var userInfo = connectionUri.UserInfo.Split(':', 2);

            connectionStringBuilder.Host = connectionUri.Host;
            connectionStringBuilder.Username = userInfo[0];
            connectionStringBuilder.Password = userInfo[1];
            connectionStringBuilder.Database = connectionUri.LocalPath.TrimStart('/');

            if (connectionUri.Port > 0)
            {
                connectionStringBuilder.Port = connectionUri.Port;
            }
            connectionStringBuilder.SslMode = Npgsql.SslMode.Require;
        }
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionStringBuilder.ToString()));
            
        return services;
    }

    public static IServiceCollection AddHotChocolate(this IServiceCollection services)
    {
        services.AddGraphQLServer()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddTypeExtension<RecipeMutation>()
                .AddTypeExtension<ProfileMutation>()
                .AddTypeExtension<RatingMutation>()
                .AddType<UploadType>()
                .AddTypeExtension<UserResolvers>()
                .AddTypeExtension<RecipeResolver>()
                .AddTypeExtension<StepResolver>()
                .AddProjections()
                .AddFiltering()
                .AddSorting()
                .AddAuthorization()
                .ModifyCostOptions(options =>
                {
                    options.MaxFieldCost = int.MaxValue;
                    options.MaxTypeCost = int.MaxValue;
                });

        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddIdentity<ReceptoriaUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            });
        services.AddScoped<TokenService>();

        return services;
    }

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        WebApplicationBuilder builder)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                string? corsOrigins = builder.Configuration["Cors:AllowedOrigins"];

                if (string.IsNullOrEmpty(corsOrigins))
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        policy.WithOrigins("https://localhost:5050", "http://localhost:5050")
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    }
                    return;
                }
                var origins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries);
                policy.WithOrigins(origins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        return services;
    }
}
