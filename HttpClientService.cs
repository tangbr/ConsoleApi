using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ConsoleApi
{
    public abstract class HttpClientService<T> : IDisposable
      where T : class
    {
        private readonly HttpClient _httpClient;
        private readonly UrlService _urlService;
        private readonly ILogger<HttpClientService<T>> _logger;

        public HttpClientService(UrlService urlService,
            ILogger<HttpClientService<T>> logger)
        {
            _urlService = urlService??
                throw new ArgumentNullException(nameof(urlService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(_urlService.BaseAddress);
        }

        private async Task<string> GetInternalAsync(string requestUri)
        {
            if (requestUri == null) throw new ArgumentNullException(nameof(requestUri));
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            HttpResponseMessage resp = await _httpClient.GetAsync(requestUri);
            LogInformation($"status from GET {resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }

        private void LogInformation(string message,
            [CallerMemberName] string callerName = null) =>
            _logger.LogInformation(
                $"{nameof(HttpClientService<T>)}.{callerName}:{message}");

        public async virtual Task<T> GetAsync(string requestUri)
        {
            string json = await GetInternalAsync(requestUri);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async virtual Task<IEnumerable<T>> GetAllAsync(string requestUri)
        {
            string json = await GetInternalAsync(requestUri);
            return JsonConvert.DeserializeObject<IEnumerable<T>>(json);
        }

        public async Task<XElement> GetAllXmlAsync(string requestUri)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri(_urlService.BaseAddress);
                client.DefaultRequestHeaders.Accept.Add(
                  new MediaTypeWithQualityHeaderValue("application/xml"));
                HttpResponseMessage resp = await client.GetAsync(requestUri);
                Console.WriteLine($"status from GET {resp.StatusCode}");
                resp.EnsureSuccessStatusCode();
                string xml = await resp.Content.ReadAsStringAsync();
                XElement chapters = XElement.Parse(xml);
                return chapters;
            }
        }

        public async Task<T> PostAsync(string uri, T item)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            string json = JsonConvert.SerializeObject(item);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");
            HttpResponseMessage resp = await _httpClient.PostAsync(uri, content);
            Console.WriteLine($"status from POST {resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
            Console.WriteLine($"added resource at {resp.Headers.Location}");
            json = await resp.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task PutAsync(string uri, T item)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            string json = JsonConvert.SerializeObject(item);
            HttpContent content = new StringContent(json, Encoding.UTF8,
              "application/json");
            HttpResponseMessage resp = await _httpClient.PutAsync(uri, content);
            Console.WriteLine($"status from PUT {resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteAsync(string uri)
        {
            if (_objectDisposed) throw new ObjectDisposedException(nameof(_httpClient));

            HttpResponseMessage resp = await _httpClient.DeleteAsync(uri);
            Console.WriteLine($"status from DELETE {resp.StatusCode}");
            resp.EnsureSuccessStatusCode();
        }

        #region IDisposable Support
        private bool _objectDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_objectDisposed)
            {
                if (disposing)
                {
                    _httpClient?.Dispose();
                }
                _objectDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
