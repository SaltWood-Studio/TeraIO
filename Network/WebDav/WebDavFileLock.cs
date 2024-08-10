using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TeraIO.Network.WebDav
{
    public class WebDavFileLock : IDisposable
    {
        public string FileName { get; protected set; }
        public string? LockToken { get; protected set; }
        private bool disposed;

        public HttpClient Client { get; protected set; }

        public WebDavFileLock(string name)
        {
            this.FileName = name;
            this.Client = new HttpClient();
        }

        public async Task Lock(int timeout = 60)
        {
            HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("LOCK"), this.FileName)
            {
                Content = new StringContent("<D:lockinfo xmlns:D='DAV:'><D:lockscope><D:exclusive/></D:lockscope><D:locktype><D:write/></D:locktype></D:lockinfo>")
            };
            message.Headers.Add("Timeout", $"Second-{timeout}");
            var response = await this.Client.SendAsync(message);
            string lockToken = response.Headers.GetValues("Lock-Token").First();
        }

        public async Task Unlock()
        {
            HttpRequestMessage message = new HttpRequestMessage(new HttpMethod("UNLOCK"), this.FileName);
            message.Headers.Add("Lock-Token", this.LockToken);
            this.LockToken = null;
            await this.Client.SendAsync(message);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // TODO: 释放托管状态(托管对象)
                }

                this.Unlock().Wait();
                disposed = true;
            }
        }

        // TODO: 仅当“Dispose(bool disposing)”拥有用于释放未托管资源的代码时才替代终结器
        ~WebDavFileLock()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 不要更改此代码。请将清理代码放入“Dispose(bool disposing)”方法中
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
