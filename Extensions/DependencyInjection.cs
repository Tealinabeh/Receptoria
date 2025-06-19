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
    public static IServiceCollection AddNpgsql(this IServiceCollection services, WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        if (builder.Environment.IsDevelopment())
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            return services;
        }

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found for production environment.");
        }

        var connectionUri = new Uri(connectionString);
        var userInfo = connectionUri.UserInfo.Split(':', 2);

        var connectionStringBuilder = new Npgsql.NpgsqlConnectionStringBuilder
        {
            Host = connectionUri.Host,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = connectionUri.LocalPath.TrimStart('/'),
            SslMode = Npgsql.SslMode.Require
        };

        if (connectionUri.Port > 0)
        {
            connectionStringBuilder.Port = connectionUri.Port;
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionStringBuilder.ToString()));

        return services;
    }
    [Obsolete]
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (string.IsNullOrEmpty(redisConnectionString))
        {
            Console.WriteLine("Redis connection string not found. Falling back to in-memory cache for development.");
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "ReceptoriaAPI_";
            });
        }

        services.AddSingleton<ICacheService, CacheService>();

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
        const string englishLetters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string ukrainianLetters = "абвгґдеєжзиіїйклмнопрстуфхцчшщьюяАБВГҐДЕЄЖЗИІЇЙКЛМНОПРСТУФХЦЧШЩЬЮЯ";
        const string russianLetters = "абвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
        const string digits = "0123456789";
        const string specialChars = @"-._@+!""#№$;%^:&?*()[]{}<>/|\~`";

        services
            .AddIdentity<ReceptoriaUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;

                options.User.AllowedUserNameCharacters =
                englishLetters + ukrainianLetters + russianLetters + digits + specialChars;

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
