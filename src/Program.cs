using FluentValidation;
using FluentValidation.AspNetCore;
using HealthChecks.UI.Client;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using SACA;
using SACA.Authorization;
using SACA.Data;
using SACA.Extensions;
using SACA.Interfaces;
using SACA.Middlewares;
using SACA.Options;
using SACA.Repositories;
using SACA.Repositories.Interfaces;
using SACA.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options => options
    .UseNpgsql(connectionString)
    .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<MapperlyMapper>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IImageRepository, ImageRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnityOfWork, UnityOfWork>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IImageService, ImageService>();
builder.Services.AddScoped<IS3Service, S3Service>();

builder.Services.AddTransient<ExceptionHandlerMiddleware>();

builder.Services.AddIdentityCoreConfiguration();
builder.Services.AddDataProtection();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

builder.Services.AddCustomCors();

builder.Services.AddOptions<AppOptions>().Bind(builder.Configuration.GetSection(nameof(AppOptions)));
builder.Services.AddOptions<AWSOptions>().Bind(builder.Configuration.GetSection(nameof(AWSOptions)));
builder.Services.AddOptions<CloudinaryOptions>().Bind(builder.Configuration.GetSection(nameof(CloudinaryOptions)));

builder.Services.AddJwtSecurity();

builder.Services.AddProblemDetails(configure => { configure.IncludeExceptionDetails = (_, _) => true; });

builder.Services.AddHealthChecks().AddNpgSql(connectionString);

builder.Services.AddSwagger();

builder.Services.AddRouting(options => options.LowercaseUrls = true);

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddHttpContextAccessor();

ValidatorOptions.Global.LanguageManager.Enabled = true;
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddControllers();

var app = builder.Build();

#if !DEBUG
app.SeedDatabaseAsync().GetAwaiter().GetResult();
#endif

if (app.Environment.IsDevelopment())
{
    app.UseCustomSwagger();

    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}

app.UseProblemDetails();

app.UseRouting();

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers().RequireAuthorization();
});

app.Run();

public partial class Program { }