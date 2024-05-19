using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TeraIO.Network.WebDav
{
    public class WebDavClient
    {
        protected HttpClient httpClient;
        public string? lockToken;

        public WebDavClient(string baseUrl)
        {
            this.httpClient = new HttpClient();
            this.httpClient.BaseAddress = new Uri(baseUrl);
        }

        public void SetUser(string username, string password)
        {
            this.httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"))}");
        }

        public async Task<HttpResponseMessage> PutFile(string fileName, byte[] data)
        {
            HttpContent content = new ByteArrayContent(data);
            if (!string.IsNullOrWhiteSpace(lockToken))
            {
                content.Headers.Add("Lock-Token", lockToken);
                content.Headers.Add("If", $"({lockToken})");
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

        public string GetFileDownloadLink(string filePath)
        {
            //TODO: GetFileDownloadLink
            throw new NotImplementedException("TODO: GetFileDownloadLink");
        }

        public async Task<HttpResponseMessage> Delete(string name)
        {
            return await this.httpClient.DeleteAsync(name);
        }

        public async Task<HttpResponseMessage> Lock(string name, int timeout = 60)
        {
            HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("LOCK"), name)
            {
                Content = new StringContent("<D:lockinfo xmlns:D='DAV:'><D:lockscope><D:exclusive/></D:lockscope><D:locktype><D:write/></D:locktype></D:lockinfo>")
            };
            message.Headers.Add("Timeout", $"Second-{timeout}");
            var response = await this.httpClient.SendAsync(message);
            this.lockToken = response.Headers.GetValues("Lock-Token").First();
            return response;
        }

        public async Task<HttpResponseMessage> Unlock(string name)
        {
            HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("UNLOCK"), name);
            message.Headers.Add("Lock-Token", this.lockToken);
            this.lockToken = null;
            return await this.httpClient.SendAsync(message);
        }
    }
}
