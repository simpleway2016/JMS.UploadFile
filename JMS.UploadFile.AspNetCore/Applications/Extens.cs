using JMS.UploadFile.AspNetCore;
using JMS.UploadFile.AspNetCore.Applications;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace Microsoft.AspNetCore.Builder
{
    public static class Extens
    {
        /// <summary>
        /// 启用JMS.UploadFile
        /// </summary>
        /// <typeparam name="T">用于接收上传数据的类</typeparam>
        /// <param name="app"></param>
        /// <param name="option">设置项</param>
        /// <returns></returns>
        public static IApplicationBuilder UseJmsUploadFile<T>(this IApplicationBuilder app, Option option) where T : IUploadFileReception
        {
            if (option == null)
                throw new ArgumentException("option is null");

            app.UseWebSockets();

            var requestReception = new RequestReception();
            if (Global.AllOptions.Count == 0)
            {
                app.Use(async (context, next) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        var path = context.Request.Path.Value;
                        if (path.StartsWith("/"))
                            path = path.Substring(1);
                        if (Global.AllOptions.TryGetValue(path, out Option currentOption))
                        {
                            WebSocket webSocket;
                            string protocol = null;
                            if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out StringValues protocols))
                            {
                                protocol = protocols.FirstOrDefault();
                                if (string.IsNullOrWhiteSpace(protocol))
                                {
                                    protocol = null;
                                }
                            }
                            
                            if (protocol != null) {
                            
                                webSocket = await context.WebSockets.AcceptWebSocketAsync(protocol);
                            }
                            else
                            {
                                webSocket = await context.WebSockets.AcceptWebSocketAsync();
                            }
                            await requestReception.Interview(context, webSocket, currentOption);
                            return;
                        }
                    }

                    await next();
                });
            }
            option.ReceptionType = typeof(T);
            Global.AllOptions[option.RouteName] = option;
            
            return app;
        }
    }
}
