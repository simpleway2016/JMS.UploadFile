"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.JMSUploadFile = void 0;
/**
 * 如果要实现断点续传，并且JMSUploadFile是一个新的实例化对象，可以通过设置tranId和serverReceived属性来实现
 * 如果是同一个JMSUploadFile对象，在传输过程中发生错误中断，只需要再次调用该对象的upload方法即可
 * */
var JMSUploadFile = /** @class */ (function () {
    /**
     *
     * @param fileEle
     * @param routeName 路由名称
     * @param serverUrl 服务器地址，如：http://www.test.com，如果为空，则以location.href为准
     */
    function JMSUploadFile(fileEle, routeName, serverUrl) {
        if (serverUrl === void 0) { serverUrl = undefined; }
        /**事务id，相同的事务id，可以实现断点续传*/
        this.tranId = "";
        /**服务器已经接收的数量*/
        this.serverReceived = 0;
        this.isUploading = false;
        this.readedPosition = 0;
        this.sendedFirstMesssage = false;
        if (fileEle.data && fileEle.filename) {
            var obj = fileEle;
            this.dataProvider = new ArrayBufferProvider(obj);
        }
        else if (fileEle.slice) {
            this.dataProvider = new FileProvider(fileEle);
        }
        else {
            this.dataProvider = new InputFileProvider(fileEle);
        }
        if (!serverUrl) {
            if (location.port)
                serverUrl = location.protocol + "//" + location.hostname + ":" + location.port + "/";
            else
                serverUrl = location.protocol + "//" + location.hostname + "/";
        }
        else {
            if (serverUrl.indexOf("/") != serverUrl.length - 1)
                serverUrl += "/";
        }
        serverUrl = serverUrl.toLocaleLowerCase();
        if (serverUrl.indexOf("http://") == 0) {
            this.serverUrl = "ws://";
            serverUrl = serverUrl.replace("http://", "");
        }
        else if (serverUrl.indexOf("https://") == 0) {
            this.serverUrl = "wss://";
            serverUrl = serverUrl.replace("https://", "");
        }
        else {
            this.serverUrl = "";
        }
        this.serverUrl += serverUrl + routeName;
        console.log("WebSocket Address:" + this.serverUrl);
    }
    JMSUploadFile.prototype.initWebSocket = function () {
        var _this = this;
        this.webSocket = new WebSocket(this.serverUrl, this.secWebSocketProtocol);
        var originalType = this.webSocket.binaryType;
        this.webSocket.onerror = function (ev) {
            _this.onSocketError();
        };
        this.webSocket.onclose = function () {
            _this.onSocketError();
        };
        this.webSocket.onopen = function () {
            _this.webSocket.send(JSON.stringify({
                filename: _this.dataProvider.name,
                length: _this.dataProvider.size,
                position: _this.serverReceived,
                tranid: _this.tranId,
                state: _this.state,
                auth: _this.auth
            }));
        };
        this.sendedFirstMesssage = false;
        this.webSocket.onmessage = function (ev) {
            if (ev.data.indexOf("{") == 0) {
                var err;
                eval("err=" + ev.data);
                _this.onerror(err);
                return;
            }
            if (!_this.sendedFirstMesssage) {
                _this.sendedFirstMesssage = true;
                _this.onFirstMessage(ev);
            }
            else {
                _this.serverReceived = parseInt(ev.data);
                if (_this.serverReceived == -1) {
                    var web = _this.webSocket;
                    _this.webSocket.onmessage = null;
                    _this.webSocket.onerror = null;
                    _this.webSocket.onclose = null;
                    _this.webSocket = null;
                    web.close();
                    _this.isUploading = false;
                }
                if (_this.onProgress && _this.serverReceived >= 0) {
                    _this.onProgress(_this, _this.dataProvider.size, _this.serverReceived);
                }
                if (_this.serverReceived == -1) {
                    _this.readedPosition = 0;
                    _this.serverReceived = 0;
                    var mytranId = _this.tranId;
                    _this.tranId = "";
                    if (_this.onCompleted != null) {
                        _this.onCompleted(_this, mytranId);
                    }
                }
            }
        };
    };
    JMSUploadFile.prototype.senddata = function () {
        var _this = this;
        this.dataProvider.read(this.readedPosition, 20480, function (filedata, err) {
            if (err) {
                _this.onerror(err);
                return;
            }
            _this.webSocket.send(filedata);
            _this.readedPosition += filedata.byteLength;
            if (_this.readedPosition >= _this.dataProvider.size) {
                return;
            }
            _this.senddata();
        });
    };
    JMSUploadFile.prototype.onFirstMessage = function (ev) {
        this.tranId = ev.data;
        this.webSocket.binaryType = "arraybuffer";
        this.senddata();
    };
    JMSUploadFile.prototype.onSocketError = function () {
        if (!this.webSocket)
            return;
        this.onerror(new Error("websocket error"));
    };
    JMSUploadFile.prototype.onerror = function (err) {
        if (!this.webSocket)
            return;
        var web = this.webSocket;
        this.webSocket.onmessage = null;
        this.webSocket.onerror = null;
        this.webSocket.onclose = null;
        this.webSocket = null;
        if (web)
            web.close();
        if (err && err.code == 6004) {
            //不支持断点续传
            this.tranId = "";
            this.serverReceived = 0;
            this.sendedFirstMesssage = false;
            this.upload();
            return;
        }
        this.isUploading = false;
        if (this.onError) {
            this.onError(this, err);
        }
    };
    JMSUploadFile.prototype.upload = function () {
        if (this.isUploading)
            throw new Error("is uploading");
        this.isUploading = true;
        this.dataProvider.init();
        this.readedPosition = this.serverReceived;
        this.initWebSocket();
    };
    return JMSUploadFile;
}());
exports.JMSUploadFile = JMSUploadFile;
var InputFileProvider = /** @class */ (function () {
    function InputFileProvider(fileEle) {
        this.element = fileEle;
    }
    InputFileProvider.prototype.init = function () {
        this.file = this.element.files[0];
        this.name = this.file.name;
        this.size = this.file.size;
        this.reader = new FileReader();
    };
    InputFileProvider.prototype.read = function (position, length, callback) {
        var _this = this;
        try {
            this.reader.onload = function (ev) {
                var filedata = _this.reader.result;
                if (callback)
                    callback(filedata, undefined);
            };
            this.reader.onerror = function (ev) {
                if (callback)
                    callback(undefined, new Error("read file error"));
            };
            var blob = this.file.slice(position, position + length);
            this.reader.readAsArrayBuffer(blob);
        }
        catch (e) {
            if (callback)
                callback(undefined, e);
        }
    };
    return InputFileProvider;
}());
var ArrayBufferProvider = /** @class */ (function () {
    function ArrayBufferProvider(obj) {
        this.obj = obj;
    }
    ArrayBufferProvider.prototype.init = function () {
        this.name = this.obj.filename;
        this.size = this.obj.data.byteLength;
    };
    ArrayBufferProvider.prototype.read = function (position, length, callback) {
        try {
            var toread = length;
            if (position + toread > this.obj.data.byteLength)
                toread = this.obj.data.byteLength - position;
            var ret = this.obj.data.slice(position, position + toread);
            if (callback)
                callback(ret, undefined);
        }
        catch (e) {
            if (callback)
                callback(undefined, e);
        }
    };
    return ArrayBufferProvider;
}());
var FileProvider = /** @class */ (function () {
    function FileProvider(obj) {
        this.obj = obj;
    }
    FileProvider.prototype.init = function () {
        this.name = this.obj.name;
        this.size = this.obj.size;
        this.reader = new FileReader();
    };
    FileProvider.prototype.read = function (position, length, callback) {
        var _this = this;
        try {
            this.reader.onload = function (ev) {
                var filedata = _this.reader.result;
                if (callback)
                    callback(filedata, undefined);
            };
            this.reader.onerror = function (ev) {
                if (callback)
                    callback(undefined, new Error("read file error"));
            };
            var blob = this.obj.slice(position, position + length);
            this.reader.readAsArrayBuffer(blob);
        }
        catch (e) {
            if (callback)
                callback(undefined, e);
        }
    };
    return FileProvider;
}());
//# sourceMappingURL=JMSUploadFile.js.map