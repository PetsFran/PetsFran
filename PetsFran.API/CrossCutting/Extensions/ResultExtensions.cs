using System.Text.Json;
using Microsoft.AspNetCore.Http;
using PetsFran.API.CrossCutting.Results;

namespace PetsFran.API.CrossCutting.Middleware;

/// <summary>
/// Extension para converter Results em respostas HTTP
/// Padroniza todas as respostas da API
/// </summary>
public static class ResultExtensions
{
    public static async Task WriteResultAsync(this HttpResponse response, Result result)
    {
        response.ContentType = "application/json";

        if (result.IsSuccess)
        {
            response.StatusCode = StatusCodes.Status200OK;
        }
        else
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await response.WriteAsJsonAsync(result, options);
    }

    public static async Task WriteResultAsync<T>(this HttpResponse response, Result<T> result, int createdStatusCode = StatusCodes.Status200OK)
    {
        response.ContentType = "application/json";

        if (result.IsSuccess)
        {
            response.StatusCode = createdStatusCode;
        }
        else
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await response.WriteAsJsonAsync(result, options);
    }
}

/// <summary>
/// DTO padrão de resposta
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse FromResult(Result result)
        => new()
        {
            Success = result.IsSuccess,
            Message = result.Message,
            Errors = result.Errors
        };

    public static ApiResponse FromResult<T>(Result<T> result)
        => new()
        {
            Success = result.IsSuccess,
            Message = result.Message,
            Data = result.Data,
            Errors = result.Errors
        };
}

