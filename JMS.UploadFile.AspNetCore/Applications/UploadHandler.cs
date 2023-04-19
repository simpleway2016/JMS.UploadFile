using JMS.UploadFile.AspNetCore.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Way.Lib;

namespace JMS.UploadFile.AspNetCore.Applications
{
    internal class UploadHandler
    {
        IUploadFileReception _uploadFileReception;
        UploadHeader _uploadHeader;
        Option _option;
        public UploadHandler(Option option, UploadHeader uploadHeader, IUploadFileReception uploadFileReception)
        {
            this._uploadFileReception = uploadFileReception;
            this._uploadHeader = uploadHeader;
            this._option = option;
        }

        public async Task Handle(HttpContext httpContext,WebSocket webSocket)
        {
            await _uploadFileReception.OnBeginUploadFile(_uploadHeader, _uploadHeader.Position == 0 ? false : true);
            var lastReportTime = DateTime.Now.AddSeconds(-100);
            var bs = new byte[10240];
            bool finished = false;
            while (true)
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(bs);
                        WebSocketReceiveResult result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                        if (result.CloseStatus != null)
                        {
                            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived || webSocket.State == WebSocketState.CloseSent)
                            {
                                await webSocket.CloseAsync(webSocket.CloseStatus.GetValueOrDefault(), "", CancellationToken.None);
                            }
                            break;
                        }
                        if (result.MessageType == WebSocketMessageType.Binary)
                        {
                           await this.onReceived(this._uploadHeader.TranId.GetValueOrDefault(), _uploadHeader.FileName, buffer.Array, result.Count, _uploadHeader.Position);
                            _uploadHeader.Position += result.Count;

                            if (_uploadHeader.Position > _uploadHeader.Length)
                            {
                                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "文件超过原大小", CancellationToken.None);
                                break;
                            }
                            else if (_uploadHeader.Position == _uploadHeader.Length)
                            {
                                await this.onFinish(this._uploadHeader.TranId.GetValueOrDefault(), _uploadHeader.FileName);
                                finished = true;

                                await sendString(webSocket, "-1");
                            }
                            else
                            {
                                if ((DateTime.Now - lastReportTime).TotalSeconds >= 1)
                                {
                                    lastReportTime = DateTime.Now;
                                    await sendString(webSocket, _uploadHeader.Position.ToString());
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        await sendString(webSocket, new { code = (int)ErrorCode.ServerError, message = ex.Message }.ToJsonString());
                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            if (!finished)
            {
                try
                {
                    await _uploadFileReception.OnError(_uploadHeader);
                }
                catch (Exception ex)
                {
                    httpContext.RequestServices.GetService<ILogger<UploadHandler>>()?.LogError(ex, "");
                }
            }
        }

        Task sendString(WebSocket webSocket,string text)
        {
            var output = new ArraySegment<byte>(Encoding.UTF8.GetBytes(text));

            return webSocket.SendAsync(output, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        Task onReceived(int tranid, string filename, byte[] data, int length, long filePosition)
        {
            return _uploadFileReception.OnReceivedFileContent(_uploadHeader, data, length, filePosition);
        }

        Task onFinish(int tranid, string filename)
        {
            return _uploadFileReception.OnUploadCompleted(_uploadHeader);
        }
    }
}
