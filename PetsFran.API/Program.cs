using System;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PetsFran.API.Data;
using PetsFran.API.Services;
using Scalar.AspNetCore;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PetsFran.API.Middleware;
using PetsFran.API.Services.Auth;
using PetsFran.API.CrossCutting.DependencyInjection;

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
            Title = "PetsFran API",
            Version = "v1",
            Description = "API para gestão de PetShops"
        };
        return Task.CompletedTask;
    });
});

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// CONFIGURAÇÃO DO BANCO DE DADOS - VERSÃO ÚNICA E OTIMIZADA
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString) || connectionString == "__CONNECTION_STRING__")
        throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada");
    options.UseNpgsql(connectionString);
});

// Configurar JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings["Secret"]
             ?? throw new InvalidOperationException("JWT Secret não configurado no appsettings.json");
if (secret == "__JWT_SECRET__" || secret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret deve ter pelo menos 32 caracteres");

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

// ✨ NOVO: Registrar serviços da Clean Architecture
builder.Services.AddCleanArchitectureServices();

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            var allowedOrigins = new List<string>();
            
            if (builder.Environment.IsDevelopment())
            {
                allowedOrigins.Add("http://localhost:4200");
            }
            else
            {
                // Frontend público em produção
                allowedOrigins.Add("https://petsfran.vercel.app");
            }
            
            policy.WithOrigins(allowedOrigins.ToArray())
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials(); 
        });
});

var app = builder.Build();

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("PetsFran API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithPreferredScheme("Bearer")
            .WithHttpBearerAuthentication(bearer => { bearer.Token = "seu-token-jwt-aqui"; });
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "PetsFran API V1");
        options.RoutePrefix = "swagger";
    });
}

// IMPORTANTE: CORS deve vir antes de outros middlewares
app.UseCors("AllowFrontend");

// Aplicar migrações antes de aceitar tráfego.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    // Provisionamento pontual: habilitado apenas enquanto ambas as variáveis estiverem presentes.
    var seedEmail = app.Configuration["PetShopSeed:Email"];
    var seedPasswordHash = app.Configuration["PetShopSeed:PasswordHash"];
    if (!string.IsNullOrWhiteSpace(seedEmail) || !string.IsNullOrWhiteSpace(seedPasswordHash))
    {
        if (string.IsNullOrWhiteSpace(seedEmail) ||
            string.IsNullOrWhiteSpace(seedPasswordHash) ||
            !seedPasswordHash.StartsWith("$2", StringComparison.Ordinal))
            throw new InvalidOperationException("Configuração de PetShopSeed incompleta ou inválida");

        var petShop = await dbContext.PetShops.SingleOrDefaultAsync(p => p.Email == seedEmail);
        if (petShop is null)
        {
            dbContext.PetShops.Add(new PetsFran.API.Models.Entities.PetShop
            {
                Email = seedEmail,
                Senha = seedPasswordHash
            });
        }
        else
        {
            petShop.Senha = seedPasswordHash;
            petShop.RefreshTokenHash = null;
            petShop.RefreshTokenExpiryTime = null;
        }

        await dbContext.SaveChangesAsync();
    }
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TokenRefreshMiddleware>();
app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503));
app.MapControllers();

app.Run();
