using JMS.UploadFile.AspNetCore;
using JMS.UploadFile.AspNetCore.Applications;
using System.IO;

namespace WebApplication1
{
    public class MyUploadReception : IUploadFileReception
    {
        FileStream fs;
        public void OnBeginUploadFile(UploadHeader header, bool isContinue)
        {
            fs = File.OpenWrite($"./{header.FileName}");
        }

        public void OnError(UploadHeader header)
        {
           
        }

        public void OnReceivedFileContent(UploadHeader header, byte[] data, int length, long filePosition)
        {
            fs.Write(data, 0, length);
        }

        public void OnUploadCompleted(UploadHeader header)
        {
            fs.Close();
        }
    }
}
