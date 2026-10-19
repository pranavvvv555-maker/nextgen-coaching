using System.Net.Http.Json;
using NextGenCoaching.Shared.Responses;

namespace NextGenCoaching.Web.Services;

/// <summary>
/// Thin wrapper over HttpClient that unwraps ApiResponse&lt;T&gt; envelopes.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(HttpClient http, ILogger<ApiClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string path, CancellationToken ct = default)
    {
        try
        {
            var envelope = await _http.GetFromJsonAsync<ApiResponse<T>>(path, ct);
            return envelope is { Success: true } ? envelope.Data : default;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GET {Path} failed", path);
            return default;
        }
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PostAsJsonAsync(path, body, ct);
            return await ReadResponseAsync<TResponse>(resp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POST {Path} failed", path);
            return ApiResponse<TResponse>.Fail("Network error. Please try again.");
        }
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PatchAsJsonAsync(path, body, ct);
            return await ReadResponseAsync<TResponse>(resp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PATCH {Path} failed", path);
            return ApiResponse<TResponse>.Fail("Network error. Please try again.");
        }
    }

    public async Task<ApiResponse<TResponse>?> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.PutAsJsonAsync(path, body, ct);
            return await ReadResponseAsync<TResponse>(resp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PUT {Path} failed", path);
            return ApiResponse<TResponse>.Fail("Network error. Please try again.");
        }
    }

    public async Task<ApiResponse<object>?> DeleteAsync(string path, CancellationToken ct = default)
    {
        try
        {
            using var resp = await _http.DeleteAsync(path, ct);
            return await ReadResponseAsync<object>(resp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DELETE {Path} failed", path);
            return ApiResponse<object>.Fail("Network error. Please try again.");
        }
    }

    /// <summary>Posts a single file as multipart/form-data (reuses the authenticated HttpClient).</summary>
    public async Task<ApiResponse<TResponse>?> UploadAsync<TResponse>(
        string path, Stream content, string fileName, string contentType, string fieldName = "file", CancellationToken ct = default)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(content);
            if (!string.IsNullOrWhiteSpace(contentType))
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            form.Add(fileContent, fieldName, fileName);

            using var resp = await _http.PostAsync(path, form, ct);
            return await ReadResponseAsync<TResponse>(resp, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UPLOAD {Path} failed", path);
            return ApiResponse<TResponse>.Fail("Upload error. Please try again.");
        }
    }
    public async Task<byte[]?> DownloadAsync(string path, CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync(path, ct);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(ct) : null;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Download failed"); return null; }
    }

    private static async Task<ApiResponse<T>> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return ApiResponse<T>.Ok(default!);

        if (response.Content.Headers.ContentType?.MediaType == "application/json")
        {
            try
            {
                var envelope = await response.Content.ReadFromJsonAsync<ApiResponse<T>>(cancellationToken: ct);
                if (envelope is not null && (response.IsSuccessStatusCode || !string.IsNullOrWhiteSpace(envelope.Message)))
                    return !envelope.Success && envelope.Errors is { Count: > 0 }
                        ? ApiResponse<T>.Fail(string.Join(" ", envelope.Errors), envelope.Errors)
                        : envelope;
            }
            catch (System.Text.Json.JsonException) { }
        }

        var message = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Invalid email or password, or your session has expired.",
            System.Net.HttpStatusCode.Forbidden => "You do not have permission to perform this action.",
            System.Net.HttpStatusCode.Conflict => "This record already exists or was changed. Please refresh and try again.",
            System.Net.HttpStatusCode.TooManyRequests => "Too many attempts. Please wait a moment and try again.",
            _ => "The server could not complete the request. Please try again."
        };
        return ApiResponse<T>.Fail(message);
    }
}
