using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace IndyMindy.Services
{
    public interface IHttpService
    {
        Task<T> GetAsync<T>(string endpoint);
        Task<T> PostAsync<T>(string endpoint, object data);
        Task<T> PutAsync<T>(string endpoint, object data);
        Task<bool> DeleteAsync(string endpoint);
        void SetAuthToken(string token);
        void ClearAuthToken();
    }

    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonOptions;

        public HttpService()
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(ApiConfig.BaseUrl);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            HttpDebugger.LogInfo($"HttpService initialized with base URL: {ApiConfig.BaseUrl}");
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            var startTime = DateTime.UtcNow;
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            
            try
            {
                HttpDebugger.LogRequest("GET", fullUrl, GetHeadersString(), "");
                
                var response = await _httpClient.GetAsync(endpoint);
                var duration = DateTime.UtcNow - startTime;
                
                var responseContent = await response.Content.ReadAsStringAsync();
                HttpDebugger.LogResponse("GET", fullUrl, (int)response.StatusCode, GetResponseHeadersString(response), responseContent, duration);
                
                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("GET", fullUrl, ex, duration);
                throw new HttpServiceException($"HTTP GET request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("GET", fullUrl, ex, duration);
                throw new HttpServiceException($"Failed to deserialize response: {ex.Message}", ex);
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            var startTime = DateTime.UtcNow;
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                HttpDebugger.LogRequest("POST", fullUrl, GetHeadersString(), json);
                
                var response = await _httpClient.PostAsync(endpoint, content);
                var duration = DateTime.UtcNow - startTime;
                
                var responseContent = await response.Content.ReadAsStringAsync();
                HttpDebugger.LogResponse("POST", fullUrl, (int)response.StatusCode, GetResponseHeadersString(response), responseContent, duration);
                
                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("POST", fullUrl, ex, duration);
                throw new HttpServiceException($"HTTP POST request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("POST", fullUrl, ex, duration);
                throw new HttpServiceException($"Failed to serialize/deserialize data: {ex.Message}", ex);
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            var startTime = DateTime.UtcNow;
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                HttpDebugger.LogRequest("PUT", fullUrl, GetHeadersString(), json);
                
                var response = await _httpClient.PutAsync(endpoint, content);
                var duration = DateTime.UtcNow - startTime;
                
                var responseContent = await response.Content.ReadAsStringAsync();
                HttpDebugger.LogResponse("PUT", fullUrl, (int)response.StatusCode, GetResponseHeadersString(response), responseContent, duration);
                
                response.EnsureSuccessStatusCode();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("PUT", fullUrl, ex, duration);
                throw new HttpServiceException($"HTTP PUT request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("PUT", fullUrl, ex, duration);
                throw new HttpServiceException($"Failed to serialize/deserialize data: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            var startTime = DateTime.UtcNow;
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            
            try
            {
                HttpDebugger.LogRequest("DELETE", fullUrl, GetHeadersString(), "");
                
                var response = await _httpClient.DeleteAsync(endpoint);
                var duration = DateTime.UtcNow - startTime;
                
                var responseContent = await response.Content.ReadAsStringAsync();
                HttpDebugger.LogResponse("DELETE", fullUrl, (int)response.StatusCode, GetResponseHeadersString(response), responseContent, duration);
                
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                var duration = DateTime.UtcNow - startTime;
                HttpDebugger.LogError("DELETE", fullUrl, ex, duration);
                throw new HttpServiceException($"HTTP DELETE request failed: {ex.Message}", ex);
            }
        }

        public void SetAuthToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                HttpDebugger.LogInfo($"Auth token set: {token.Substring(0, Math.Min(20, token.Length))}...");
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
                HttpDebugger.LogInfo("Auth token cleared");
            }
        }

        public void ClearAuthToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            HttpDebugger.LogInfo("Auth token cleared");
        }

        private string GetHeadersString()
        {
            var sb = new StringBuilder();
            foreach (var header in _httpClient.DefaultRequestHeaders)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            return sb.ToString();
        }

        private string GetResponseHeadersString(HttpResponseMessage response)
        {
            var sb = new StringBuilder();
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            foreach (var header in response.Content.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(", ", header.Value)}");
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class HttpServiceException : Exception
    {
        public HttpServiceException(string message) : base(message) { }
        public HttpServiceException(string message, Exception innerException) : base(message, innerException) { }
    }
} 