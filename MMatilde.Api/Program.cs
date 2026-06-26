using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MMatilde.Api.Data;
using MMatilde.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

//
// =========================
// CONFIGURATION (ORDEN CORRECTO)
// =========================
//

// 1. Config base del proyecto (SIEMPRE)
builder.Configuration.AddJsonFile(
    "appsettings.json",
    optional: true,
    reloadOnChange: true
);

// 2. Config por entorno (Development / Production)
builder.Configuration.AddJsonFile(
    $"appsettings.{builder.Environment.EnvironmentName}.json",
    optional: true,
    reloadOnChange: true
);

// 3. Config externa VPS (override final)
var externalConfigPath = @"C:\Deploy\.appsetings\Mmerceria\appsettings.json";

if (File.Exists(externalConfigPath))
{
    builder.Configuration.AddJsonFile(
        externalConfigPath,
        optional: true,
        reloadOnChange: true
    );
}

//
// =========================
// VALIDACIÓN MÍNIMA (EVITA 500 SILENCIOSOS)
// =========================
//

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn))
{
    throw new Exception("Missing ConnectionString: DefaultConnection");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new Exception("Missing Jwt:Key configuration");
}

//
// =========================
// DATABASE
// =========================
//

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(conn));

//
// =========================
// HTTP CLIENT
// =========================
//

builder.Services.AddHttpClient<MakorScraperService>()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        UseCookies = true,
        CookieContainer = new System.Net.CookieContainer()
    });

builder.Services.AddScoped<SyncService>();

//
// =========================
// CONTROLLERS
// =========================
//

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//
// =========================
// CORS
// =========================
//

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

//
// =========================
// JWT AUTH
// =========================
//

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey!)
            )
        };
    });

var app = builder.Build();

//
// =========================
// PIPELINE
// =========================
//

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//
// =========================
// SEED
// =========================
//

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var config = services.GetRequiredService<IConfiguration>();
    await SeedData.Initialize(services, config);
}

app.Run();
