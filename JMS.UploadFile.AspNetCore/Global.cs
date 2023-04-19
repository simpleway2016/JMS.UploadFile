using System;
using System.Collections.Generic;
using System.Text;

namespace JMS.UploadFile.AspNetCore
{
    public static class Global
    {
        public static Way.Lib.Collections.IgnoreCaseDictionary AllOptions { get; set; } = new Way.Lib.Collections.IgnoreCaseDictionary();
        public static IServiceProvider ServiceProvider { get; set; }
    }
}
