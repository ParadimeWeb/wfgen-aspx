using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace ParadimeWeb.WorkflowGen.Data.GraphQL
{
    public class Client
    {
        public static string DefaultUrl => $"{VirtualPathUtility.RemoveTrailingSlash(ConfigurationManager.AppSettings["ApplicationUrl"])}/graphql";
        private static HttpClient httpClient;
        private static HttpClientHandler httpClientHandler;
        public static void CreateClient(string url, ICredentials credentials = null)
        {
            if (httpClientHandler == null)
            {
                httpClientHandler = new HttpClientHandler { Credentials = credentials ?? CredentialCache.DefaultNetworkCredentials };
                httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(url) };
            }
        }

        public static dynamic Query(string query, Variable[] variables, string impersonateUsername = null)
        {
            dynamic result;
            var json = new JObject();
            using (var request = new HttpRequestMessage(HttpMethod.Post, string.Empty))
            {
                if (impersonateUsername != null)
                {
                    request.Headers.Add("x-wfgen-impersonate-username", impersonateUsername);
                }
                if (variables != null && variables.Length > 0)
                {
                    var variablesJson = new JObject();
                    var queryParams = new List<string>();
                    foreach (var variable in variables)
                    {
                        queryParams.Add($"${variable.Name}: {variable.Type}");
                        variablesJson.Add(variable.Name, variable.Value);
                    }
                    query = $"query ({string.Join(", ", queryParams)}) {{ {query} }}";
                    json.Add("query", query);
                    json.Add("variables", variablesJson);
                }
                else
                {
                    json.Add("query", $"{{ {query} }}");
                }
                request.Content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
                using (var response = httpClient.SendAsync(request).Result)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Status: {response.StatusCode}, request Url: {request.RequestUri}");
                    }
                    result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                    if (result.errors != null)
                    {
                        throw new Exception((string)result.errors[0].message);
                    }
                }
            }

            return result;
        }
        public static dynamic Query(string query, string impersonateUsername = null) => Query(query, null, impersonateUsername);
        public static dynamic Mutation(string query, Variable[] variables, string impersonateUsername = null)
        {
            dynamic result;
            var json = new JObject();
            if (variables != null && variables.Length > 0)
            {
                var mutationParams = new List<string>();
                var variablesJson = new JObject();
                var multipartMapContentJson = new JObject();
                var multipartFileUploads = new List<MultipartFileUpload>();
                foreach (var variable in variables)
                {
                    mutationParams.Add($"${variable.Name}: {variable.Type}");
                    variablesJson.Add(variable.Name, variable.Value);
                    if (variable is MultipartFileUpload)
                    {
                        multipartFileUploads.Add(variable as MultipartFileUpload);
                        multipartMapContentJson.Add(variable.Name, new JArray() { $"variables.{variable.Name}" });
                    }
                }
                query = $"mutation ({string.Join(", ", mutationParams)}) {{ {query} }}";
                json.Add("query", query);
                json.Add("variables", variablesJson);

                if (multipartFileUploads.Count > 0)
                {
                    var fileContents = new List<ByteArrayContent>();
                    try
                    {
                        using (var operationsContent = new StringContent(JsonConvert.SerializeObject(json)))
                        using (var mapContent = new StringContent(JsonConvert.SerializeObject(multipartMapContentJson)))
                        using (var multipartContent = new MultipartFormDataContent
                        {
                            {operationsContent, "operations"},
                            {mapContent, "map"}
                        })
                        {
                            foreach (var fileUpload in multipartFileUploads)
                            {
                                var fileContent = new ByteArrayContent(fileUpload.Content);
                                fileContent.Headers.ContentType = new MediaTypeHeaderValue(fileUpload.ContentType);
                                fileContents.Add(fileContent);
                                multipartContent.Add(fileContent, fileUpload.Name, fileUpload.FileName);
                            }

                            using (var request = new HttpRequestMessage(HttpMethod.Post, string.Empty))
                            {
                                request.Headers.Accept.Add(
                                    new MediaTypeWithQualityHeaderValue("multipart/form-data")
                                );
                                if (impersonateUsername != null)
                                {
                                    request.Headers.Add("x-wfgen-impersonate-username", impersonateUsername);
                                }
                                request.Content = multipartContent;
                                using (var response = httpClient.SendAsync(request).Result)
                                {
                                    if (response.StatusCode != HttpStatusCode.OK)
                                    {
                                        throw new Exception($"Status: {response.StatusCode}, request Url: {request.RequestUri}");
                                    }
                                    result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                                    if (result.errors != null)
                                    {
                                        throw new Exception((string)result.errors[0].message);
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        foreach (var fileContent in fileContents)
                        {
                            fileContent.Dispose();
                        }
                    }
                    return result;
                }
            }
            else
            {
                json.Add("query", $"mutation {{ {query} }}");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, string.Empty))
            {
                if (impersonateUsername != null)
                {
                    request.Headers.Add("x-wfgen-impersonate-username", impersonateUsername);
                }
                request.Content = new StringContent(JsonConvert.SerializeObject(json), Encoding.UTF8, "application/json");
                using (var response = httpClient.SendAsync(request).Result)
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Status: {response.StatusCode}, request Url: {request.RequestUri}");
                    }
                    result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
                }
            }
            return result;
        }
        public static dynamic Mutation(string query, string impersonateUsername = null) => Mutation(query, null, impersonateUsername);
        public static void UpdateFormData(int processInstId, DataSet formData, string formDataName = "FORM_DATA")
        {
            byte[] formDataBytes;
            using (var stream = new MemoryStream())
            {
                formData.WriteXml(stream, XmlWriteMode.WriteSchema);
                formDataBytes = stream.ToArray();
            }
            Mutation(
                @"updateRequestDataset(input: { 
    number: " + processInstId + @", 
    parameters: [{ name: """ + formDataName + @""", fileValue: { upload: $formData } }] 
}) { clientMutationId }",
                new MultipartFileUpload[] {
                    new MultipartFileUpload("formData", "dataOUT.xml", "application/xml", formDataBytes)
                });
        }
        public static void UpdateFormData(int processInstId, DataSet formData, byte[] formArchive, string formDataName = "FORM_DATA", string formArchiveName = "FORM_ARCHIVE")
        {
            byte[] formDataBytes;
            using (var stream = new MemoryStream())
            {
                formData.WriteXml(stream, XmlWriteMode.WriteSchema);
                formDataBytes = stream.ToArray();
            }
            Mutation(
                @"updateRequestDataset(input: { 
    number: " + processInstId + @", 
    parameters: [
        { name: """ + formDataName + @""", fileValue: { upload: $formData } },
        { name: """ + formArchiveName + @""", fileValue: { upload: $formArchive } }
    ] 
}) { clientMutationId }",
                new MultipartFileUpload[] {
                    new MultipartFileUpload("formData", "dataOUT.xml", "application/xml", formDataBytes),
                    new MultipartFileUpload("formArchive", "form_archive.htm", "text/html", formArchive)
                });
        }
    }
}
