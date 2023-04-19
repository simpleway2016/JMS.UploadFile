using JMS.UploadFile.AspNetCore;
using JMS.UploadFile.AspNetCore.Applications;
using System.IO;
using System.Threading.Tasks;
namespace WebApplication1
{
    public class MyUploadReception : IUploadFileReception
    {
        FileStream fs;
        public Task OnBeginUploadFile(UploadHeader header, bool isContinue)
        {
            fs = File.OpenWrite($"./{header.FileName}");
            return Task.CompletedTask;
        }

        public Task OnError(UploadHeader header)
        {
           return Task.CompletedTask;
        }

        public async Task OnReceivedFileContent(UploadHeader header, byte[] data, int length, long filePosition)
        {
            await fs.WriteAsync(data, 0, length);
        }

        public async Task OnUploadCompleted(UploadHeader header)
        {
            fs.Close();
        }
    }
}
