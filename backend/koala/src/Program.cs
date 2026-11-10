using koala.src.Shared;
using Scalar.AspNetCore;
using koala.src.Modules.Account;
using koala.src.Modules.Cms;
using koala.src.Modules.Core;
using koala.src.Shared.Validation;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();


// MODULES INIT
builder.Services.AddSharedModule(configuration);
builder.Services.AddAccountModule(configuration);
builder.Services.AddCmsModule(configuration);
builder.Services.AddCoreModule(configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddFluentValidationRulesToSwagger();

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
    app.UseSwagger();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseExceptionHandler();

CmsModuleExtensions.AddCmsModule(app, app.Configuration);
SharedExtension.AddSharedModule(app, app.Configuration);

app.MapControllers();

app.Run();