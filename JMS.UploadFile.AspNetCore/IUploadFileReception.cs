﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JMS.UploadFile.AspNetCore
{
    /// <summary>
    /// 处理上传的文件
    /// </summary>
    public interface IUploadFileReception
    {
       
        /// <summary>
        /// 开始文件的传输
        /// </summary>
        /// <param name="header"></param>
        /// <param name="isContinue">是否断点续传</param>
        Task OnBeginUploadFile(UploadHeader header, bool isContinue);

        /// <summary>
        /// 接收到文件内容
        /// </summary>
        /// <param name="header"></param>
        /// <param name="data"></param>
        /// <param name="length">data的长度</param>
        /// <param name="filePosition">接收到的数据所在的position</param>
        Task OnReceivedFileContent(UploadHeader header, byte[] data, int length, long filePosition);

        /// <summary>
        /// 文件上传完毕
        /// </summary>
        /// <param name="header"></param>
        Task OnUploadCompleted(UploadHeader header);

        /// <summary>
        /// 传输错误
        /// </summary>
        /// <param name="header"></param>
        Task OnError(UploadHeader header);
    }
}
