using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using TeraIO.Runnable;

namespace TeraIO.Network.WebDav
{
    public class WebDavClient
    {
        protected HttpClient httpClient;

        public WebDavClient(string baseUrl)
        {
            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void SetUser(string username, string password)
        {
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}");
        }

        public async Task<HttpResponseMessage> PutFile(string fileName, byte[] data, WebDavFileLock? fileLock = null)
        {
            HttpContent content = new ByteArrayContent(data);
            if (!string.IsNullOrWhiteSpace(fileLock?.LockToken))
            {
                content.Headers.Add("Lock-Token", fileLock?.LockToken);
                content.Headers.Add("If", $"({fileLock?.LockToken})");
            }
            return await this.httpClient.PutAsync(fileName, content);
        }

        public async Task<HttpResponseMessage> PutFile(string fileName, Stream stream, WebDavFileLock? fileLock = null)
        {
            HttpContent content = new StreamContent(stream);
            if (!string.IsNullOrWhiteSpace(fileLock?.LockToken))
            {
                content.Headers.Add("Lock-Token", fileLock?.LockToken);
                content.Headers.Add("If", $"({fileLock?.LockToken})");
            }
            return await this.httpClient.PutAsync(fileName, content);
        }

        public async Task<HttpResponseMessage> CreateFolder(string folderName)
        {
            return await this.httpClient.SendAsync(new HttpRequestMessage(new HttpMethod("MKCOL"), folderName));
        }

        public async Task<bool> Exists(string name)
        {
            return (await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, name))).IsSuccessStatusCode;
        }

        public async Task<long> GetFileSize(string name)
        {
            var resp = await this.httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, name));
            if (!resp.IsSuccessStatusCode)
            {
                return 0;
            }
            if (resp.Content.Headers.TryGetValues("Content-Length", out var fileSize))
            {
                return long.Parse(fileSize.First());
            }
            return 0;
        }

        public async Task<List<string>> ListFilesAndFolders(string folderPath, int depth = -1)
        {
            var requestMessage = new HttpRequestMessage(new HttpMethod("PROPFIND"), folderPath)
            {
                Headers =
                {
                    { "Depth", depth.ToString() }
                },
                Content = new StringContent(string.Empty)
            };
            if (depth < 0)
            {
                requestMessage.Headers.Remove("Depth");
            }
            requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");

            var response = await httpClient.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParsePropFindResponse(responseContent);
        }

        private List<string> ParsePropFindResponse(string responseContent)
        {
            XDocument document = XDocument.Parse(responseContent);
            XNamespace ns = "DAV:";

            List<string> items = new List<string>();

            foreach (var response in document.Descendants(ns + "response"))
            {
                var hrefElement = response.Element(ns + "href");
                if (hrefElement != null)
                {
                    items.Add(hrefElement.Value);
                }
            }

            return items;
        }

        public string GetFileDownloadLink(string filePath)
        {
            // Ensure the file path is correctly URL encoded
            string encodedFilePath = HttpUtility.UrlEncode(filePath);

            // Construct the full URL for downloading the file
            string url = $"{httpClient.BaseAddress}{encodedFilePath}";

            // Determine if the URL should use https or http
            string protocol = url.StartsWith("https", StringComparison.OrdinalIgnoreCase) ? "https" : "http";

            // Check the Authorization header for Basic auth and modify the URL accordingly
            if (httpClient.DefaultRequestHeaders.Authorization != null &&
                httpClient.DefaultRequestHeaders.Authorization.Scheme.Equals("Basic", StringComparison.OrdinalIgnoreCase))
            {
                string? authPart = httpClient.DefaultRequestHeaders.Authorization.Parameter;
                string authContents = Encoding.UTF8.GetString(Convert.FromBase64String(authPart ?? ""));
                url = url.Replace($"{protocol}://", $"{protocol}://{authContents}@");
            }

            return url;
        }


        public async Task<HttpResponseMessage> Delete(string name)
        {
            return await this.httpClient.DeleteAsync(name);
        }

        public async Task<WebDavFileLock> Lock(string name, int timeout = 60)
        {
            WebDavFileLock fileLock = new WebDavFileLock(name);
            fileLock.Client.BaseAddress = this.httpClient.BaseAddress;
            fileLock.Client.DefaultRequestHeaders.Clear();
            this.httpClient.DefaultRequestHeaders.ForEach(kvp => fileLock.Client.DefaultRequestHeaders.Add(kvp.Key, kvp.Value));
            await fileLock.Lock();
            return fileLock;
        }

        public async Task<byte[]> GetFile(string filename)
        {
            return await (await this.httpClient.GetAsync(filename)).Content.ReadAsByteArrayAsync();
        }

        public async Task<Stream> GetFileStream(string filename)
        {
            return await (await this.httpClient.GetAsync(filename)).Content.ReadAsStreamAsync();
        }

        public async Task Unlock(WebDavFileLock fileLock)
        {
            await fileLock.Unlock();
        }
    }
}
