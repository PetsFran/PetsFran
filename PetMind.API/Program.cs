using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetMind.API.Data;
using PetMind.API.Services;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PetMind.API.Middleware;
using PetMind.API.Services.Auth;

var builder = WebApplication.CreateBuilder(args);

// Configurar serviços
builder.Services.AddControllers();

// Configurar OpenAPI/Swagger
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "PetMind API",
            Version = "v1",
            Description = "API para gestão de PetShops"
        };
        return Task.CompletedTask;
    });
});

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Configurar banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                           ?? Environment.GetEnvironmentVariable("DATABASE_URL");

    options.UseNpgsql(connectionString);
});

// Configurar JWT Authentication (seu código existente)
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"]
             ?? throw new InvalidOperationException("JWT Secret não configurado no appsettings.json");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var principal = context.Principal;
                Console.WriteLine("=== JWT VALIDATION SUCCESS ===");
                Console.WriteLine($"User authenticated: {principal?.Identity?.IsAuthenticated}");
                Console.WriteLine($"User name: {principal?.Identity?.Name}");

                if (principal != null)
                {
                    foreach (var claim in principal.Claims)
                    {
                        Console.WriteLine($"  Claim: {claim.Type} = {claim.Value}");
                    }

                    var nameId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var email = principal.FindFirst(ClaimTypes.Email)?.Value;
                    var petShopId = principal.FindFirst("PetShopId")?.Value;

                    Console.WriteLine($"NameIdentifier: {nameId}");
                    Console.WriteLine($"Email: {email}");
                    Console.WriteLine($"PetShopId: {petShopId}");
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("=== JWT VALIDATION FAILED ===");
                Console.WriteLine($"Exception: {context.Exception.Message}");
                Console.WriteLine($"Exception type: {context.Exception.GetType().Name}");

                if (context.Exception is SecurityTokenExpiredException)
                {
                    Console.WriteLine("Token EXPIRED!");
                }
                else if (context.Exception is SecurityTokenInvalidSignatureException)
                {
                    Console.WriteLine("Invalid signature!");
                }

                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                Console.WriteLine($"=== JWT CHALLENGE ===");
                Console.WriteLine($"Error: {context.Error}");
                Console.WriteLine($"ErrorDescription: {context.ErrorDescription}");
                Console.WriteLine($"Request Path: {context.Request.Path}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Registrar serviços
builder.Services.AddScoped<IValidaRacaService, ValidaRacaService>();
builder.Services.AddScoped<CalculaPrecosService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpContextAccessor();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            if (builder.Environment.IsDevelopment())
            {
                policy.WithOrigins(
                        "http://localhost:4200"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
            else
            {
                // Em produção, usar apenas o domínio do frontend
                policy.WithOrigins(
                        "https://petsfranagendamento.vercel.app"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
        });
});

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Scalar UI - acessar em /scalar/v1
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("PetMind API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithPreferredScheme("Bearer")
            .WithHttpBearerAuthentication(bearer => { bearer.Token = "seu-token-jwt-aqui"; });
    });

    // Swagger UI alternativo (opcional)
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PetMind API V1");
        options.RoutePrefix = "swagger";
    });

    app.UseCors("AllowFrontend");
}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Aplica as migrações automaticamente
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("Migrações aplicadas com sucesso!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao aplicar migrações: {ex.Message}");
        // Não falha a aplicação se as migrações não funcionarem
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TokenRefreshMiddleware>();
app.MapControllers();

app.Run();