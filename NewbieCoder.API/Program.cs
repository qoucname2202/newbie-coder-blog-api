using NewbieCoder.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);


var app = builder.Build();


app.UseApiPipeline();

app.Run();

// Expose for integration tests
public partial class Program;
