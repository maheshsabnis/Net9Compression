using System.Text;
using System.Text.Json;
using Core_CompressionAPI.Middlewares;
using Core_CompressionAPI.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("cors",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1000 * 1024 * 1024; // 1000MB
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1000 * 1024 * 1024; // 1000MB
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("cors");    
// Serve default wwwroot
app.UseStaticFiles();
// Serve your custom Files folder
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
    Path.Combine(builder.Environment.ContentRootPath, "Files")),
    RequestPath = "/Files"
});
app.UseMiddleware<CompressionMiddleware>();

app.MapPost("/api/upload/data", async (HttpContext context) =>
{
    try
    {
        var bodySizeFeature = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature != null && !bodySizeFeature.IsReadOnly)
        {
            bodySizeFeature.MaxRequestBodySize = null; // Disable limit
        }

        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        
        var json = await reader.ReadToEndAsync();

        var fileData = JsonSerializer.Deserialize<FileData>(json);
        string fileName = fileData?.FileName;

        if (string.IsNullOrEmpty(fileName))
        {
            throw new InvalidOperationException("FileName is missing from the uploaded data.");
        }

        var filePath = Path.Combine(builder.Environment.ContentRootPath, "Files", fileName);

        // Save the JSON content from FileData instead of copying an empty stream
        await File.WriteAllTextAsync(filePath, fileData.Content); // Changed back to save original JSON content

        await context.Response.WriteAsync("File uploaded successfully.");
    }
    catch (Exception ex)
    {
        await context.Response.WriteAsync($"File uploaded failed: {ex.Message}.");
    }
});

app.MapPost("/api/upload/file", async (HttpContext context) =>
{
    var random = new Random();
    var filePath = Path.Combine(builder.Environment.ContentRootPath,"Files", $"File-{random.Next(100)}.json");
    await using var fileStream = File.Create(filePath);
    await context.Request.Body.CopyToAsync(fileStream);
    await context.Response.WriteAsync("File uploaded successfully.");
});




app.Run();

 