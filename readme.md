大文件上传处理中间件，支持断点续传

引用 nuget 包：JMS.UploadFile.AspNetCore

先定义一个接收类：
``` cs
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
```
然后在 app.Run 之前注册这个接收类
``` cs
 app.UseJmsUploadFile<MyUploadReception>(new JMS.UploadFile.AspNetCore.Option("uploadtest"));

```

**Html页面的使用**

``` html
<body>
    <input id="file" type="file" />
    <button onclick="obj.upload()">
        upload
    </button>
    <div id="info"></div>
</body>
<script lang="ja">
    var info = document.body.querySelector("#info");

    //引用nodejs模块
    var JMSUploadFile = require("jms-uploadfile");
    var fileObj = document.body.querySelector("#file");
    var obj = new JMSUploadFile(fileObj , "uploadtest");
    obj.onProgress = function (sender, total, sended) {
        info.innerHTML = sended + "," + total;
    }
    obj.onCompleted = function (sender) {
        info.innerHTML = "ok";
    }
    obj.onError = function (sender, err) {
        info.innerHTML = JSON.stringify( err );
        //如果断点续传，这里直接调用obj.upload()即可
    }
</script>
```

***TypeScript in webpack***
tsconfig.json
```
{
  "compilerOptions": {
    "outDir": "./dist/",
    "sourceMap": true,
    "noImplicitAny": false,
    "module": "es2015",
    "moduleResolution": "node",
    "target": "es5",
    "allowJs": true,
    "types": [
      "./node_modules/jack-websocket-uploadfile",
    ]
  }
}

```
**import**
```
import WebSocketUploadFile from "jack-websocket-uploadfile"

```
