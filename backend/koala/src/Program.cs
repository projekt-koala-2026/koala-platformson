using koala.src.Shared;
using Scalar.AspNetCore;
using koala.src.Modules.Account;
using koala.src.Modules.Cms;
using koala.src.Modules.Core;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// MODULES INIT
builder.Services.AddSharedModule(builder.Configuration);
builder.Services.AddAccountModule(builder.Configuration);
builder.Services.AddCmsModule(builder.Configuration);
builder.Services.AddCoreModule(builder.Configuration);

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

var app = builder.Build();

CmsModuleExtensions.AddCmsModule(app);

//app.UseCors("AllowFrontend");

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


app.MapControllers();

// app.MapGet("/health", () => Results.Ok("Healthy"));
// app.Urls.Add("http://0.0.0.0:8080");


app.Run();