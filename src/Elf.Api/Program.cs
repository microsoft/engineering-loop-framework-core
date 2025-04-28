using Elf.Api.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Engineering Loop Framework (ELF) API",
        Version = "v1",
        Description = "Core ELF API that supports GitHub Enterprise integration."
    });

    options.EnableAnnotations();

    // Include XML comments
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    // Force SwaggerOperation to take precedence over XML comments
    options.CustomOperationIds(apiDesc => null);     
});

builder.Services.AddOpenApi();

builder.Services.AddSingleton<GitHubIssuesService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ELF API v1");
        options.RoutePrefix = string.Empty; 
    });        
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

