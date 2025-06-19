using Microsoft.EntityFrameworkCore;
using Receptoria.API.Data;
using Receptoria.API.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.Services.AddNpgsql(builder);
builder.Services.AddRedisCache(configuration);
builder.Services.AddSecurity(configuration);
builder.Services.AddMemoryCache();

builder.Services.AddHotChocolate();
builder.Services.AddControllers();

builder.Services.AddCustomCors(builder);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();
    await DbInitializer.InitializeAsync(services);
}

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL();
app.MapControllers();

app.Run();