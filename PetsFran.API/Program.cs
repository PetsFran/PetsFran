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
    string connectionString = null;

    // 1. Tentar obter da variável de ambiente DATABASE_URL (Railway)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

    if (!string.IsNullOrEmpty(databaseUrl))
    {
        try
        {
            Console.WriteLine($"DATABASE_URL encontrada: {databaseUrl.Substring(0, databaseUrl.IndexOf('@') + 1)}***");

            // Formato esperado: postgresql://usuario:senha@host:porta/banco
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            var username = userInfo[0];
            var password = userInfo[1];
            var database = uri.AbsolutePath.TrimStart('/');

            // Construir connection string no formato correto para Npgsql
            connectionString = $"Host={uri.Host};" +
                               $"Port={uri.Port};" +
                               $"Database={database};" +
                               $"Username={username};" +
                               $"Password={password};" +
                               $"SSL Mode=Require;" +
                               $"Trust Server Certificate=true;";

            Console.WriteLine("String de conexão construída com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar DATABASE_URL: {ex.Message}");
            // Fallback: tentar usar a string do appsettings
            connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        }
    }
    else
    {
        // 2. Fallback para connection string do appsettings.json
        connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        Console.WriteLine("Usando DefaultConnection do appsettings.json");
    }

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("Nenhuma string de conexão configurada");
    }

    // Log seguro (escondendo a senha)
    var maskedConnection = connectionString;
    if (maskedConnection.Contains("Password="))
    {
        var parts = maskedConnection.Split(';');
        for (int i = 0; i < parts.Length; i++)
        {
            if (parts[i].StartsWith("Password="))
            {
                parts[i] = "Password=***";
                break;
            }
        }
        maskedConnection = string.Join(";", parts);
    }
    Console.WriteLine($"Connection string configurada: {maskedConnection}");

    options.UseNpgsql(connectionString);
});

// Configurar JWT Authentication
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
            var allowedOrigins = new List<string>();
            
            if (builder.Environment.IsDevelopment())
            {
                allowedOrigins.Add("http://localhost:4200");
            }
            else
            {
                allowedOrigins.Add("https://pets-fran-agendamento-seven.vercel.app/login");
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

// Aplicar migrações automaticamente
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
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