using System;
using System.Collections.Generic;
using System.Text;

namespace JMS.UploadFile.AspNetCore.Dtos
{
    enum ErrorCode : int
    {
        TooBig = 601,
        ServerError = 500,
        NoContinue = 603,
        NotAllowResume = 604

    }
}
