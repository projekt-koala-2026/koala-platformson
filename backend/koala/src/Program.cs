using koala.src.Shared;
using Scalar.AspNetCore;
using koala.src.Modules.Account;
using koala.src.Modules.Cms;
using koala.src.Modules.Core;
using koala.src.Shared.Validation;


var builder = WebApplication.CreateBuilder(args);

IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

builder.Services.AddOpenApi();

// MODULES INIT
builder.Services.AddSharedModule(configuration);
builder.Services.AddAccountModule(configuration);
builder.Services.AddCmsModule(configuration);
builder.Services.AddCoreModule(configuration);

// ALL ROUTING IS LOWER CASE
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

// ADD CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddControllers(options =>
{ options.Filters.AddService<FluentValidationFilter>(); }
);

var app = builder.Build();


app.UseCors("AllowFrontend");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    //using (var scope = app.Services.CreateScope())
    //{
    //    var dbContext = scope.ServiceProvider.GetRequiredService<AccountDbContext>();
    //    await dbContext.Database.EnsureCreatedAsync();
    //}
}

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

CmsModuleExtensions.AddCmsModule(app, app.Configuration);
SharedExtension.AddSharedModule(app, app.Configuration);

app.MapControllers();

// app.MapGet("/health", () => Results.Ok("Healthy"));
// app.Urls.Add("http://0.0.0.0:8080");


app.Run();