using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.WebSockets;
using WebUi.Models;

namespace WebUi
{
    public class WsOverview : IHttpHandler
    {
        public WsOverview()
        {
            ProcessInfoCache.OnCacheUpdated += ProcessInfoOnCacheOnOnCacheUpdated;
        }

        private void ProcessInfoOnCacheOnOnCacheUpdated(Overview overview)
        {
            SendUpdatedList(overview);
        }

        public void ProcessRequest(HttpContext context)
        {
            if (context.IsWebSocketRequest)
                context.AcceptWebSocketRequest(WebSocketRequest);
            else
            {
                context.Response.StatusCode = 500;
                context.Response.Write("not supporded!");
                return;
            }

            if (Clients.Count == 0)
            {
                ProcessInfoCache.Start();
            }
        }

        private static readonly List<WebSocket> Clients = new List<WebSocket>();
        private static readonly ReaderWriterLockSlim Locker = new ReaderWriterLockSlim();

        private async Task SendUpdatedList(Overview overview)
        {
            var json = new JavaScriptSerializer().Serialize(overview);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

            //Передаём сообщение всем клиентам
            for (var i = 0; i < Clients.Count; i++)
            {

                var client = Clients[i];

                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }

                catch (ObjectDisposedException)
                {
                    Locker.EnterWriteLock();
                    try
                    {
                        Clients.Remove(client);
                        i--;
                    }
                    finally
                    {
                        Locker.ExitWriteLock();
                    }

                    if (Clients.Count == 0)
                    {
                        ProcessInfoCache.Stop();
                    }
                }
                catch (Exception e)
                {
                    var a = 1;
                }
            }
        }

        private Task WebSocketRequest(AspNetWebSocketContext context)
        {
            var socket = context.WebSocket;

            Locker.EnterWriteLock();
            try
            {
                Clients.Add(socket);
            }
            finally
            {
                Locker.ExitWriteLock();
            }

            while (true)
            {
                if (socket.State == WebSocketState.Open) continue;

                Locker.EnterWriteLock();
                try
                {
                    Clients.Remove(socket);
                }
                finally
                {
                    Locker.ExitWriteLock();
                }
            }
        }

        public bool IsReusable => false;
    }
}