using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Xml.Linq;
using System;
using JMS.Token;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class MainController : ControllerBase
    {
        TokenClient _tokenClient;
        public MainController(TokenClient tokenClient)
        {
            this._tokenClient = tokenClient;
        }
        [HttpGet]
        public string GetToken()
        {           
            var expireTime = DateTime.Now.AddMinutes(20);//20分钟过期

            //生成token
            var token = _tokenClient.Build("abc", expireTime);
            return token;
        }
    }
}
