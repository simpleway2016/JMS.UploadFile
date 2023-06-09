﻿using JMS.UploadFile.AspNetCore.Dtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Way.Lib;
using static Microsoft.AspNetCore.Hosting.Internal.HostingApplication;
using static System.Net.Mime.MediaTypeNames;

namespace JMS.UploadFile.AspNetCore.Applications
{
    internal class RequestReception
    {
        static int transcationId = 0;

        async Task<ClaimsPrincipal> auth(HttpContext httpContext, WebSocket socket,Type uploadFileReceptionType,IUploadFileReception uploadFileReception)
        {
            var author = uploadFileReceptionType.GetCustomAttribute<AuthorizeAttribute>();

            if (author != null)
            {
                try
                {
                    var authRet = await httpContext.AuthenticateAsync(author.AuthenticationSchemes);

                    if (authRet.Succeeded == false)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "401,身份验证失败", CancellationToken.None);
                        return null;
                    }
                    return authRet.Principal;
                }
                catch (Exception ex)
                {
                    httpContext.RequestServices.GetService<ILogger<RequestReception>>()?.LogError(ex, "身份验证异常");
                }
            }
            return null;
        }

        /// <summary>
        /// 处理请求
        /// </summary>
        /// <param name="httpContext"></param>
        public async Task Interview(HttpContext httpContext, WebSocket socket, Option option)
        {
          
            var constructor = option.ReceptionType.GetConstructors()[0];
            var parameterInfos = constructor.GetParameters();
            var parameters = new object[parameterInfos.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                try
                {
                    parameters[i] = httpContext.RequestServices.GetService(parameterInfos[i].ParameterType);
                }
                catch (Exception ex)
                {
                    throw new Exception($"初始化{option.ReceptionType.FullName}出错,{ex.Message}");
                }
            }
            IUploadFileReception uploadFileReception = (IUploadFileReception)Activator.CreateInstance(option.ReceptionType, parameters);
            //身份验证
            var user = await auth(httpContext, socket, option.ReceptionType, uploadFileReception);

            var bs = new byte[2048];
            while (true)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        ArraySegment<byte> buffer = new ArraySegment<byte>(bs);
                        WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, CancellationToken.None);
                        if (result.CloseStatus != null)
                        {
                            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived || socket.State == WebSocketState.CloseSent)
                            {
                                await socket.CloseAsync(socket.CloseStatus.GetValueOrDefault(), "", CancellationToken.None);
                            }
                            break;
                        }

                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            string jsonString = Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
                            var header = Newtonsoft.Json.JsonConvert.DeserializeObject<UploadHeader>(jsonString);
                            if (header.Length > option.MaxFileLength)
                            {
                                var errBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(new { code = (int)ErrorCode.TooBig, message = "文件大小超过上限" }.ToJsonString()));
                                await socket.SendAsync(errBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                break;
                            }
                            if (header.Position > 0 && !option.AllowResume)
                            {
                                var errBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(new { code = (int)ErrorCode.NotAllowResume, message = "不支持断点续传" }.ToJsonString()));
                                await socket.SendAsync(errBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
                                break;
                            }
                            if (header.TranId == null)
                            {
                                header.TranId = System.Threading.Interlocked.Increment(ref transcationId);
                            }

                            var output = new ArraySegment<byte>(Encoding.UTF8.GetBytes(header.TranId.ToString()));
                            await socket.SendAsync(output, WebSocketMessageType.Text, true, CancellationToken.None);

                            bs = null;

                                                  

                            if (socket.State == WebSocketState.Open)
                            {
                                header.User = user;
                                var uploaderHandler = new UploadHandler(option, header, uploadFileReception);
                                await uploaderHandler.Handle(httpContext, socket);
                            }
                            break;

                        }
                        else
                        {
                            await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, "请求格式不正确", CancellationToken.None);
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }
}
