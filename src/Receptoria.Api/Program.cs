var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGraphQLServer()
                .AddQueryType<TestQuery>();


var app = builder.Build();
app.MapGraphQL();
app.Run();


public class TestQuery
{
    public string Hello(string name = "World") =>
                $"Hello {name}!";
}