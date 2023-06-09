/**
 * 如果要实现断点续传，并且JMSUploadFile是一个新的实例化对象，可以通过设置tranId和serverReceived属性来实现
 * 如果是同一个JMSUploadFile对象，在传输过程中发生错误中断，只需要再次调用该对象的upload方法即可
 * */
export declare class JMSUploadFile {
    serverUrl: string;
    /**事务id，相同的事务id，可以实现断点续传*/
    tranId: string;
    /**服务器已经接收的数量*/
    serverReceived: number;
    isUploading: boolean;
    /**上传到服务器的附加信息*/
    state: string;
    /**服务器校验信息*/
    auth: string;
    /**Sec-WebSocket-Protocol请求头 */
    secWebSocketProtocol: string;
    onProgress: (sender: JMSUploadFile, totalBytes: any, serverReceived: any) => void;
    onCompleted: (sender: JMSUploadFile, tranId: string) => void;
    onError: (sender: JMSUploadFile, err: any) => void;
    private webSocket;
    private dataProvider;
    private readedPosition;
    private sendedFirstMesssage;
    /**
     *
     * @param fileEle
     * @param routeName 路由名称
     * @param serverUrl 服务器地址，如：http://www.test.com，如果为空，则以location.href为准
     */
    constructor(fileEle: any, routeName: string, serverUrl?: string);
    private initWebSocket;
    private senddata;
    private onFirstMessage;
    private onSocketError;
    private onerror;
    upload(): void;
}
