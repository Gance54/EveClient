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
        }

        public async Task<T> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();
                
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpServiceException($"HTTP GET request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new HttpServiceException($"Failed to deserialize response: {ex.Message}", ex);
            }
        }

        public async Task<T> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: Sending POST to {endpoint}");
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: Request JSON: {json}");
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PostAsync(endpoint, content);
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: Response status: {response.StatusCode}");
                
                var responseContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: Response content: {responseContent}");
                
                response.EnsureSuccessStatusCode();
                
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: HttpRequestException: {ex.Message}");
                throw new HttpServiceException($"HTTP POST request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HttpService.PostAsync: JsonException: {ex.Message}");
                throw new HttpServiceException($"Failed to serialize/deserialize data: {ex.Message}", ex);
            }
        }

        public async Task<T> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await _httpClient.PutAsync(endpoint, content);
                response.EnsureSuccessStatusCode();
                
                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new HttpServiceException($"HTTP PUT request failed: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new HttpServiceException($"Failed to serialize/deserialize data: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                throw new HttpServiceException($"HTTP DELETE request failed: {ex.Message}", ex);
            }
        }

        public void SetAuthToken(string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public void ClearAuthToken()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
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