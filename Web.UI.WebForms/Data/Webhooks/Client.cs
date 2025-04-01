using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;

namespace ParadimeWeb.WorkflowGen.Data.Webhooks
{
    public class Client
    {
        private static HttpClient httpClient;
        public static void CreateClient(string url, string hookToken)
        {
            if (httpClient == null)
            {
                httpClient = new HttpClient() { BaseAddress = new Uri(url) };
                httpClient.DefaultRequestHeaders.Add("x-wfgen-hooktoken", hookToken);
            }
        }
        public static dynamic SendOpenAPI(string operation, object input)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Post, operation))
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(new { args = new { input } }), Encoding.UTF8, "application/json");
                using (var response = httpClient.SendAsync(request).Result)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Status: {response.StatusCode}, request Url: {request.RequestUri}");
                    }
                    dynamic result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (result.error != null)
                    {
                        throw new Exception((string)result.error);
                    }
                    return result;
                }
            }
        }
    }
}
