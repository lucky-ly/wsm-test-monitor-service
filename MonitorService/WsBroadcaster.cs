using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MonitorService.Entities;
using MonitorService.Interfaces;
using Newtonsoft.Json;

namespace MonitorService
{
    public class WsBroadcaster
    {
        private readonly List<WebSocket> _clients = new List<WebSocket>();
        private readonly ReaderWriterLockSlim _locker = new ReaderWriterLockSlim();

        private readonly ILogger _logger;
        private readonly ServiceConfiguration _config;

        public WsBroadcaster(ILogger logger, ServiceConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public async void Start()
        {
            var prefix = _config.WsUrl;
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(prefix);
            httpListener.Start();

            _logger.Info("WsServer started");

            while (true)
            {
                var context = await httpListener.GetContextAsync();

                if (context.Request.IsWebSocketRequest)
                {
                    ProcessRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext context)
        {
            WebSocketContext wsContext;
            try
            {
                wsContext = await context.AcceptWebSocketAsync(null);

            }
            catch (Exception)
            {
                context.Response.StatusCode = 500;
                context.Response.Close();
                return;
            }

            await ProcessWsRequest(wsContext);
        }

        private async Task ProcessWsRequest(WebSocketContext wsContext)
        {
            var socket = wsContext.WebSocket;
            _logger.Info("Ws Client connected");

            AddClient(socket);

            try
            {
                var buffer = new byte[1024];
                while (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            _logger.Info("Ws Close message received");
                            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        }
                        else
                        {
                            var outBuffer = Encoding.UTF8.GetBytes("test");
                            await socket.SendAsync(new ArraySegment<byte>(outBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            finally
            {
                socket?.Dispose();
                RemoveClient(socket);
                _logger.Info("Ws Client disconnected");
            }
        }

        private void AddClient(WebSocket socket)
        {
            _locker.EnterWriteLock();
            try
            {
                _clients.Add(socket);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        private void RemoveClient(WebSocket socket)
        {
            _locker.EnterWriteLock();
            try
            {
                _clients.Remove(socket);
            }
            finally
            {
                _locker.ExitWriteLock();
            }
        }

        public async Task SendUpdatedList(Overview overview)
        {
            var json = JsonConvert.SerializeObject(overview);
            var buffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));

            for (var i = 0; i < _clients.Count; i++)
            {

                var client = _clients[i];

                try
                {
                    if (client.State == WebSocketState.Open)
                    {
                        await client.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }

                catch (ObjectDisposedException)
                {
                    RemoveClient(client);
                    i--;
                }
                catch (Exception e)
                {
                    _logger.Error("Unexpected error while sending message to ws client", e);
                }
            }
        }
    }
}
