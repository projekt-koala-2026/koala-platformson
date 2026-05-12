using koala.Data;
using koala.Data.ViewModels;
using koala.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

//NOTE: VALIDATORS
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UserChangeEmailValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UserChangePasswordValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UserChangeRolesValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UserLoginValidator>();

builder.Services.AddFluentValidationAutoValidation();

//NOTE: CONECTING TO DB
var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
var dbPort = Environment.GetEnvironmentVariable("DB_PORT");
var dbUser = Environment.GetEnvironmentVariable("DB_USER");
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
var dbName = Environment.GetEnvironmentVariable("DB_NAME");
var connectionString =
    $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword}";

builder.Services.AddDbContextFactory<AppDbContext>(options => 
{
    options.UseNpgsql(connectionString);
});

//NOTE: ADDING AUTH
builder.Services
    .AddAuthentication("AdminPanelKoalaScheme")
    .AddScheme<AuthenticationSchemeOptions, KoalaAuthHandler>("AdminPanelKoalaScheme", options => {});
builder.Services.AddAuthorization();

//NOTE: ADDING SERVICES
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<UserService>();
builder.Services.AddTransient<FileService>();
builder.Services.AddTransient<EditionService>();
builder.Services.AddTransient<PostService>();
builder.Services.AddTransient<SponsorService>();

//NOTE: ALL ROUTING IS LOWER CASE
builder.Services.AddRouting(options =>
{
    options.LowercaseUrls = true;
});

//NOTE: ADD CORS
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
app.UseCors("AllowFrontend");

//NOTE: DELEVOPMENT MODE - ADDS OPENAPI + DB CONFIGS
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
        await authService.InicializeDB();
        var fileService = scope.ServiceProvider.GetRequiredService<FileService>();
        await fileService.CreateFolderStructure();
    }
}

//NOTE: SERVING STATIC FILES (IMAGES & TEXT FILES & PDFS)
var publicPath = Environment.GetEnvironmentVariable("PUBLIC_STORAGE_PATH");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(publicPath),
    RequestPath = "/content",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Urls.Add("http://0.0.0.0:8080");

app.Run();
