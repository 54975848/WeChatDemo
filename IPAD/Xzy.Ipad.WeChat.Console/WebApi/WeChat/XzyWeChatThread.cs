using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApi.Model;
using WebApi.Utils;
using System.Linq;
using System.Text.RegularExpressions;
using Xzy.IPAD.Core;
using System.Configuration;
using Fleck;
using WebApi.Util;
using WebApi.MyWebSocket;

namespace WebApi.WeChat
{
    public class XzyWeChatThread
    {
        #region 全局变量
        public IWebSocketConnection _socket;
        private bool WxFlag = false;
        public int pointerWxUser;
        public int pushStr;
        public int result;
        public int msgPtr;
        public int callBackMsg;

        string name = "changtuiqie-ipad";

        public int wxMeId = 0;

        public string bankerWxid = "";
        public string cheshouWxid = "";
        public string groupId = "";

        /// <summary>
        /// socket 链接状态
        /// </summary>
        public bool SocketIsConnect = false;
        /// <summary>
        /// 登录二维码
        /// </summary>
        public string ScanQrCode = "";

        /// <summary>
        /// 微信对象
        /// </summary>
        public UserData userData = new UserData();
        private WxUser wxUser = new WxUser();
        private List<WxGroup> wxGroup { get; set; }

        Dictionary<string, string> dicRedPack;
        Dictionary<string, string> dicReadContent;

        public List<Contact> wxContacts = new List<Contact>();

        public List<Contact> wxGroups = new List<Contact>();

        public List<Contact> wxGzhs = new List<Contact>();

        Random R = new Random();
        string RandomStr(int n)
        {
            List<int> ilist = new List<int>();
            for (int i = 0; i < n; i++)
            {
                ilist.Add(R.Next(0, 9));
            }
            return string.Join("", ilist);

        }
        Int64 RandomL(int n)
        {
            List<int> ilist = new List<int>();
            for (int i = 0; i < n; i++)
            {
                ilist.Add(R.Next(0, 9));
            }
            return Convert.ToInt64(string.Join("", ilist));

        }
        string Mac
        {
            get
            {
                //return "0016D3B5C493";
                int min = 0;
                int max = 16;
                Random ro = new Random();
                var sn = string.Format("{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}",
                   ro.Next(min, max).ToString("x"),//0
                   ro.Next(min, max).ToString("x"),//
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),//5
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),
                   ro.Next(min, max).ToString("x"),//10
                   ro.Next(min, max).ToString("x")
                    ).ToUpper();
                return sn;
            }
        }
        string UUID
        {
            get
            {
                return RandomStr(8) + "-" + RandomStr(4) + "-" + RandomL(4).ToString("X") + "-" + RandomL(4).ToString("X") + "-" + RandomL(12).ToString("X");
            }
        }

        #endregion

        #region 微信委托
        public XzyWxApis.DllcallBack msgCallBack { get; set; }
        #endregion

        #region 构造函数
        /// <summary>
        /// 初始化，并且使用二维码登录。并且初始化一些计时器，但实际上这些计时器没有什么用。这些计时器，应该是为了实现类似signalr的功能
        /// </summary>
        public XzyWeChatThread(IWebSocketConnection socket,string devicename)
        {
            if (devicename != "") {
                name = devicename;
            }

            _socket = socket;
            dicRedPack = new Dictionary<string, string>();
            dicReadContent = new Dictionary<string, string>();
            Task.Factory.StartNew(() => {
                this.Init();
            });

            msgCallBack += new XzyWxApis.DllcallBack(Wx_MsgCallBack);
        }

        public XzyWeChatThread(IWebSocketConnection socket,string username,string password,string str62,string devicename)
        {
            if (devicename != "") {
                name = devicename;
            }
            _socket = socket;
            dicRedPack = new Dictionary<string, string>();
            dicReadContent = new Dictionary<string, string>();
            Task.Factory.StartNew(() => {
                this.Init62(str62,username,password);
            });

            msgCallBack += new XzyWxApis.DllcallBack(Wx_MsgCallBack);
        }

        #endregion

        #region 全局方法

        #region websocket

        //websocket发送消息
        private void WebSocketSend(string msg)
        {
            try
            {
                if (_socket.IsNull() || !SocketIsConnect)
                {
                    return;
                }
                _socket.Send(msg);
            }
            catch (Exception ex)
            {
            }
        }

        private void WebSocketSendLog( string msg)
        {
            try
            {
                if (_socket.IsNull() || !SocketIsConnect)
                {
                    return;
                }
                SocketModel model = new SocketModel();
                model.action = "log";
                model.context = msg;
                _socket.Send(JsonConvert.SerializeObject(model));
            }
            catch (Exception ex)
            {
            }
        }

        #endregion 

        /// <summary>
        /// 登录
        /// </summary>
        public unsafe void Init()
        {
            try {
                fixed (int* WxUser1 = &pointerWxUser, pushStr1 = &pushStr)
                {
                    string uid = UUID;
                    var mac = Mac;

                    XzyAuth.GetIpPort();
                    var ret = XzyAuth.Init(ConfigurationManager.AppSettings["AuthKey"]);
                    WebSocketSendLog(ret);
                    var key = string.Format(App.DeviceKey, UUID, Mac);
                    XzyWxApis.WXInitialize((int)WxUser1, name, key, UUID);
                    XzyWxApis.WXSetRecvMsgCallBack(pointerWxUser, msgCallBack);
                    XzyWxApis.WXGetQRCode(pointerWxUser, (IntPtr)pushStr1);
                    var msg = Marshal.PtrToStringAnsi(new IntPtr(Convert.ToInt32(pushStr)));
                    WxQrCode qr_code = JsonConvert.DeserializeObject<WxQrCode>(msg);//反序列化
                                                                                    //var img = MyUtils.Base64StringToImage(qr_code.QrCodeStr);
                    ScanQrCode = qr_code.QrCodeStr;
                    SocketModel model = new SocketModel()
                    {
                        action = "qrcode",
                        context = qr_code.QrCodeStr
                    };
                    WebSocketSend(JsonConvert.SerializeObject(model));
                    Wx_ReleaseEX(ref pushStr);
                    QrCodeJson QRCodejson = null;
                    while (true)
                    {
                        if (WxFlag)
                        {
                            break;
                        }
                        Thread.Sleep(1000);
                        XzyWxApis.WXCheckQRCode(pointerWxUser, (IntPtr)pushStr1);
                        var datas = MarshalNativeToManaged((IntPtr)pushStr);
                        if (datas == null)
                        {
                            continue;
                        }
                        string sstr = datas.ConvertToString();
                        QRCodejson = Newtonsoft.Json.JsonConvert.DeserializeObject<QrCodeJson>(sstr);//反序列化
                        Wx_ReleaseEX(ref pushStr);
                        bool breakok = false;
                        switch (QRCodejson.Status)
                        {
                            case 0: WebSocketSendLog("请扫描二维码"); break;
                            case 1: WebSocketSendLog("请点在手机上点确认"); break;
                            case 2: WebSocketSendLog("正在登录中"); breakok = true; break;
                            case 3: WebSocketSendLog("已过期"); break;
                            case 4: WebSocketSendLog("取消操作了"); breakok = true; break;
                            case -2007: WebSocketSendLog("已过期"); return;
                        }
                        if (breakok) { break; }
                    }
                    if (QRCodejson.Status == 2)
                    {
                        var username = QRCodejson.UserName;
                        var password = QRCodejson.Password;
                        string s62 = Wx_GenerateWxDat();
                        WxDat wxDat = JsonConvert.DeserializeObject<WxDat>(s62);
                        var str62 = Convert62.eStrToHex(wxDat.data);
                        EUtils.AuthApi(username, password, UUID, Mac, name, str62);
                        Thread.Sleep(3000);
                        var strlogin = Wx_QRCodeLogin(username, password);
                        userData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(strlogin);//反序列化
                        if (userData.Status == -301)
                        {
                            var Str = Wx_QRCodeLogin(username, password);
                            userData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(Str);//反序列化
                            if (userData.Status == 0)
                            {
                                WebSocketSendLog("登录成功");
                                XzyWxApis.WXHeartBeat(pointerWxUser, (IntPtr)pushStr1);
                                var datas = MarshalNativeToManaged((IntPtr)pushStr);
                                var sstr = datas.ConvertToString();
                                Wx_ReleaseEX(ref pushStr);
                                this.wxUser.wxid = userData.UserName;
                                this.wxUser.name = userData.NickName;
                                Task.Factory.StartNew(delegate { this.Wx_GetContacts(); });
                                Wx_ReleaseEX(ref pushStr);
                                return;
                            }
                        }
                        if (userData.Status == 0)
                        {
                            WebSocketSendLog("登录成功");
                            XzyWxApis.WXHeartBeat(pointerWxUser, (IntPtr)pushStr1);
                            var datas = MarshalNativeToManaged((IntPtr)pushStr);
                            var sstr = datas.ConvertToString();
                            Wx_ReleaseEX(ref pushStr);
                            this.wxUser.wxid = userData.UserName;
                            this.wxUser.name = userData.NickName;
                            Task.Factory.StartNew(delegate { this.Wx_GetContacts(); });
                            Wx_ReleaseEX(ref pushStr);
                            return;
                        }
                        else
                        {
                            WebSocketSendLog("登录失败：" + userData.Message);
                        }
                    }
                }
            } catch (Exception ex) {

            } 
        }

        /// <summary>
        /// 初始化62数据
        /// </summary>
        /// <param name="str16"></param>
        /// <param name="WxUsername"></param>
        /// <param name="wxpassword"></param>
        public unsafe void Init62(string str16, string WxUsername, string wxpassword)
        {
            fixed (int* WxUser1 = &pointerWxUser, pushStr1 = &pushStr)
            {
                var ret = XzyAuth.Init(ConfigurationSettings.AppSettings["AuthKey"].ConvertToString());
                string uid = UUID;
                var mac = Mac;
                XzyAuth.GetIpPort();
                var key = string.Format(@"<softtype><k3>11.0.1</k3><k9>iPad</k9><k10>2</k10><k19>58BF17B5-2D8E-4BFB-A97E-38F1226F13F8</k19><k20>{0}</k20><k21>neihe_5GHz</k21><k22>(null)</k22><k24>{1}</k24><k33>\345\276\256\344\277\241</k33><k47>1</k47><k50>1</k50><k51>com.tencent.xin</k51><k54>iPad4,4</k54></softtype>", UUID, Mac);

                XzyWxApis.WXInitialize((int)WxUser1, name, key, UUID);

                XzyWxApis.WXSetRecvMsgCallBack(pointerWxUser, msgCallBack);

                //62数据是扫码登录成功后，再获取，并保存下来，而不是其它方式登录后再保存。并且还要使用方法WXGetLoginToken保存下token
                #region 使用62数据自动登录，在扫码登录后，会得到62数据及token，传入到这里即可实现自动登录
                //加载62数据
                byte[] data62Bytes = Convert.FromBase64String(str16);
                XzyWxApis.WXLoadWxDat(pointerWxUser, data62Bytes, data62Bytes.Length, (IntPtr)pushStr1);
                var datas1 = MarshalNativeToManaged((IntPtr)pushStr);
                var sstr1 = datas1.ConvertToString();
                if (string.IsNullOrEmpty(sstr1))
                {
                    WebSocketSendLog("登陆失败，重新登录");
                }
                Wx_ReleaseEX(ref pushStr);
                #endregion
                EUtils.AuthApi(WxUsername, wxpassword, UUID, Mac, name, str16);
                Thread.Sleep(3000);
                //以下是使用账号密码登录，已经测试成功。账号：13127873237，密码：Taobao123
                XzyWxApis.WXUserLogin(pointerWxUser, WxUsername, wxpassword, (IntPtr)pushStr1);
                var datas = MarshalNativeToManaged((IntPtr)pushStr);
                var sstr = datas.ConvertToString();
                Wx_ReleaseEX(ref pushStr);

                userData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(sstr);//反序列化

                if (userData.Status == -301)
                {
                    XzyWxApis.WXUserLogin(pointerWxUser, WxUsername, wxpassword, (IntPtr)pushStr1);
                    datas = MarshalNativeToManaged((IntPtr)pushStr);
                    sstr = datas.ConvertToString();
                    Wx_ReleaseEX(ref pushStr);
                    WebSocketSendLog("微信重定向");
                    userData = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(sstr);//反序列化
                    this.wxUser.wxid = userData.UserName;
                    this.wxUser.name = userData.NickName;
                    if (userData.Status == 0)
                    {
                        WebSocketSendLog("登录成功");
                        XzyWxApis.WXHeartBeat(pointerWxUser, (IntPtr)pushStr1);
                        datas = MarshalNativeToManaged((IntPtr)pushStr);
                        sstr = datas.ConvertToString();
                        Wx_ReleaseEX(ref pushStr);
                        Task.Factory.StartNew(delegate { this.Wx_GetContacts(); });

                        //：登录成功后，取出token备用
                        //XzyWxApis.WXGetLoginToken(pointerWxUser, (int)pushStr1);
                        //var datas3 = MarshalNativeToManaged((IntPtr)pushStr);
                        //var sstr3 = datas3.ConvertToString();
                        //var tokenData = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(sstr3, new { Token = "" });
                        Wx_ReleaseEX(ref pushStr);
                        return;
                    }
                    else
                    {
                        WebSocketSendLog("登录失败：" + userData.Message);
                    }
                }
                if (userData.Status == 0)
                {
                    WebSocketSendLog("登录成功");
                    XzyWxApis.WXHeartBeat(pointerWxUser, (IntPtr)pushStr1);
                    datas = MarshalNativeToManaged((IntPtr)pushStr);
                    sstr = datas.ConvertToString();
                    Wx_ReleaseEX(ref pushStr);
                    this.wxUser.wxid = userData.UserName;
                    this.wxUser.name = userData.NickName;
                    Task.Factory.StartNew(delegate { this.Wx_GetContacts(); });

                    //：登录成功后，取出token备用
                    //XzyWxApis.WXGetLoginToken(pointerWxUser, (int)pushStr1);
                    //var datas3 = MarshalNativeToManaged((IntPtr)pushStr);
                    //var sstr3 = datas3.ConvertToString();
                    //var tokenData = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(sstr3, new { Token = "" });
                    Wx_ReleaseEX(ref pushStr);
                    return;
                }
                else
                {
                    WebSocketSendLog("登录失败：" + userData.Message);
                }
            }
        }


        /// <summary>
        /// 微信返回消息解码
        /// </summary>
        /// <param name="pNativeData"></param>
        /// <returns></returns>
        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            try
            {
                if ((int)pNativeData <= 0)
                {
                    return null;
                }
                List<byte> list = new List<byte>();
                int num = 0;
                for (; ; )
                {
                    byte b = Marshal.ReadByte(pNativeData, num);
                    if (b == 0)
                    {
                        break;
                    }
                    list.Add(b);
                    num++;
                }
                return Encoding.UTF8.GetString(list.ToArray(), 0, list.Count);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 图片转byte
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public byte[] ImageToBytes(Image image)
        {
            ImageFormat format = image.RawFormat;
            using (MemoryStream ms = new MemoryStream())
            {
                if (format.Equals(ImageFormat.Jpeg))
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                else if (format.Equals(ImageFormat.Png))
                {
                    image.Save(ms, ImageFormat.Png);
                }
                else if (format.Equals(ImageFormat.Bmp))
                {
                    image.Save(ms, ImageFormat.Bmp);
                }
                else if (format.Equals(ImageFormat.Gif))
                {
                    image.Save(ms, ImageFormat.Gif);
                }
                else if (format.Equals(ImageFormat.Icon))
                {
                    image.Save(ms, ImageFormat.Icon);
                }
                byte[] buffer = new byte[ms.Length];
                //Image.Save()会改变MemoryStream的Position，需要重新Seek到Begin
                ms.Seek(0, SeekOrigin.Begin);
                ms.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        /// <summary>
        /// 打印消息
        /// </summary>
        /// <param name="msg"></param>
        private void ShowMessage(string msg)
        {
            //Console.WriteLine(msg);
        }

        public static int TimeStamp
        {
            get
            {

                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                return Convert.ToInt32(ts.TotalSeconds - 180);
            }
        }

        #endregion
        #region 微信方法

        #region 微信消息
        /// <summary>
        /// 获取视频消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public unsafe string Wx_GetMsgVideo(string msg)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetMsgVideo(pointerWxUser, msg, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 获取语音消息
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public unsafe string Wx_GetMsgVoice(string msg)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetMsgVoice(pointerWxUser, msg, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 获取图片消息
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="content"></param>
        public unsafe string Wx_GetMsgImage(string msg)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetMsgImage(pointerWxUser, msg, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 发消息 -文字
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="content"></param>
        /// <param name="atlist"></param>
        /// <returns></returns>
        public unsafe string Wx_SendMsg(string wxid, string content, List<string> atlist)
        {
            content = content.Replace(" ", "\r\n");
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                if (atlist.Count > 0)
                {
                    XzyWxApis.WXSendMsg(pointerWxUser, wxid, content, JsonConvert.SerializeObject(atlist), (IntPtr)msgptr1);
                }
                else
                {
                    XzyWxApis.WXSendMsg(pointerWxUser, wxid, content, null, (IntPtr)msgptr1);
                }
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 群发消息
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_MassMessage(string wxid, string content)
        {
            content = content.Replace(" ", "\r\n");
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXMassMessage(pointerWxUser, wxid, content, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 发消息 - 图片
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="imgpath"></param>
        public unsafe void Wx_SendImg(string wxid, string imgpath)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    Image _image = Image.FromStream(WebRequest.Create(imgpath).GetResponse().GetResponseStream());
                    //把文件读取到字节数组
                    byte[] data = this.ImageToBytes(_image);
                    if (data.Length > 0)
                    {
                        XzyWxApis.WXSendImage(pointerWxUser, wxid, data, data.Length, (IntPtr)msgptr1);
                        var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                        var str = datas.ConvertToString();
                        Wx_ReleaseEX(ref msgPtr);
                    }
                    _image = null;
                }
                catch { }
            }
        }

        /// <summary>
        /// 发消息 - 图片
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="imgpath"></param>
        public unsafe string Wx_SendImg(string wxid, Image _image)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                //把文件读取到字节数组
                byte[] data = this.ImageToBytes(_image);
                XzyWxApis.WXSendImage(pointerWxUser, wxid, data, data.Length, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return str;
            }
        }

        /// <summary>
        /// 发语音 - silk
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="imgpath"></param>
        public unsafe string Wx_SendVoice(string wxid, string silkpath, int time)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    FileStream fs = new FileStream(silkpath, FileMode.Open, FileAccess.Read);
                    //获取文件大小
                    long size = fs.Length;

                    byte[] data = new byte[size];
                    //将文件读到byte数组中
                    fs.Read(data, 0, data.Length);
                    fs.Close();
                    if (data.Length > 0)
                    {
                        XzyWxApis.WXSendVoice(pointerWxUser, wxid, data, data.Length, time * 1000, (IntPtr)msgptr1);
                        var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                        result = datas.ConvertToString();
                        Wx_ReleaseEX(ref msgPtr);
                    }
                }
                catch { }
            }
            return result;
        }

        /// <summary>
        /// 分享名片
        /// </summary>
        /// <param name="user"></param>
        /// <param name="wxid"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public unsafe string Wx_ShareCard(string user, string wxid, string title)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {

                XzyWxApis.WXShareCard(pointerWxUser, user, wxid, title.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        public unsafe string Wx_EShareCard(string user, string wxid, string title)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {

                msgPtr = EUtils.EShareCarde(pointerWxUser, user, wxid, title);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 微信消息 - 回调
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        public unsafe void Wx_MsgCallBack(int a, IntPtr b)
        {
            if (b.ConvertToString().ConvertToInt32() == -1)
            {
                string uuid = "";
                lock (XzyWebSocket._dicSockets)
                {
                    //下线注销
                    uuid = XzyWebSocket._dicSockets.Where(p => p.Value.weChatThread.wxUser == wxUser).ToList().FirstOrDefault().Key;
                    if (uuid.ConvertToString() != "")
                    {
                        WebSocketSendLog($"您的账号{uuid}已下线");
                        msgCallBack -= new XzyWxApis.DllcallBack(Wx_MsgCallBack);
                        Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(10 * 1000);
                            XzyWebSocket._dicSockets[uuid].weChatThread = null;
                            XzyWebSocket._dicSockets.Remove(uuid);
                        });
                    }
                }   
                return;
            }
            fixed (int* wxUser1 = &pointerWxUser, callBackMsg1 = &callBackMsg)
            {
                try
                {
                    XzyWxApis.WXSyncMessage(pointerWxUser, (IntPtr)callBackMsg1);
                    if (callBackMsg == 0)
                    {
                        return;
                    }
                    var str = MarshalNativeToManaged((IntPtr)callBackMsg).ConvertToString();
                    List<BackWxMsg> BackWxMsg = new List<BackWxMsg>();
                    Wx_ReleaseEX(ref callBackMsg);
                    List<WxTtsMsg> WXttsmsg = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WxTtsMsg>>(str);
                    foreach (var msg in WXttsmsg)
                    {
                        var msgtype = msg.MsgType;
                        var content = msg.Content;
                        var sub_type = msg.SubType;
                        var MsgId = msg.MsgId;
                        if (msg.Timestamp < TimeStamp)
                        {
                            continue;
                        }
                        //判断此消息是否已经处理过。若未处理，才会进入里面处理
                        if (Wx_SetMsgKey(MsgId))
                        {
                            SocketModel model = new SocketModel()
                            {
                                action = "msgcallback",
                                context = JsonConvert.SerializeObject(msg)
                            };
                            WebSocketSend(JsonConvert.SerializeObject(model));
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("异常事件");
                }
            }
        }

        public object wx_objMsg = new object();
        /// <summary>
        /// 设置消息。用于判断消息是否被处理过。若未处理过，则返回true，已经处理过的，返回false。
        /// </summary>
        /// <param name="msgid"></param>
        /// <returns></returns>
        public bool Wx_SetMsgKey(string msgid)
        {
            lock (wx_objMsg)
            {
                try
                {
                    if (dicReadContent.Count > 5000)
                    {
                        dicReadContent = new Dictionary<string, string>();
                    }

                    if (!dicReadContent.ContainsKey(msgid))
                    {
                        dicReadContent.Add(msgid, msgid);
                        return true;
                    }
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        #endregion 微信消息

        #region 微信群

        /// <summary>
        /// 取群成员
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public unsafe List<WxMember> Wx_GetGroupMember(string groupId)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetChatRoomMember(pointerWxUser, groupId, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                GroupMember groupmember = null;
                groupmember = Newtonsoft.Json.JsonConvert.DeserializeObject<GroupMember>(str);
                List<Member> member = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Member>>(groupmember.Member);
                List<WxMember> WxMember = new List<WxMember>();
                if (member != null && member.Count > 0)
                {
                    foreach (var m in member)
                    {
                        WxMember w = new WxMember();
                        w.userid = this.wxUser.wxid;
                        w.groupid = groupId;
                        w.nickname = m.NickName;
                        w.wxid = m.UserName;
                        WxMember.Add(w);
                    }
                    return WxMember;
                }
                return null;
            }
        }

        /// <summary>
        /// 进群
        /// </summary>
        /// <param name="url"></param>
        public unsafe void Wx_IntoGroup(string url)
        {
            if (url != "")
            {

                fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
                {
                    XzyWxApis.WXGetRequestToken(pointerWxUser, "", url, (IntPtr)msgptr1);
                    if ((int)msgptr1 == 0) { return; }

                    var json = MarshalNativeToManaged((IntPtr)msgPtr).ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                    if (json == "") { return; }
                    EnterGroupJson jinqunjson = Newtonsoft.Json.JsonConvert.DeserializeObject<EnterGroupJson>(json);
                    var FullUrl = jinqunjson.FullUrl;
                    var tk = Utilities.GetMidStr(jinqunjson.FullUrl + "||||", "ticket=", "||||");
                    Http_Helper Http_Helper = new Http_Helper();
                    var res = "";
                    var status = Http_Helper.GetResponse_WX(ref res, FullUrl, "POST", "", FullUrl, 30000, "UTF-8", true);
                    //WxDelegate.show("被邀请进入群，开始读通讯录！");
                    this.Wx_GetContacts();
                }
            }
        }

        public object wx_groupObj = new object();
        /// <summary>
        /// 更新群成员
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public bool Wx_SetGroup(string groupId)
        {
            lock (wx_groupObj)
            {
                try
                {
                    List<WxGroup> nWxGroup = new List<WxGroup>();

                    for (int i = 0; i < wxGroup.Count; i++)
                    {
                        WxGroup n = wxGroup[i];
                        if (groupId == n.groupid)
                        {
                            n.member = this.Wx_GetGroupMember(wxGroup[i].groupid);
                        }
                        if (n.member != null)
                        {
                            nWxGroup.Add(n);
                        }
                    }
                    wxGroup = nWxGroup;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 创建群
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public unsafe string Wx_CreateChatRoom(string users)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXCreateChatRoom(pointerWxUser, users, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                var tokenData = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(result, new { user_name = "" });
                if (!String.IsNullOrEmpty(tokenData.user_name))
                {
                    result = tokenData.user_name;
                }
                if (result.Contains("@chatroom"))
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 退群
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        public unsafe string Wx_QuitChatRoom(string groupid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXQuitChatRoom(pointerWxUser, groupid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 修改群名称
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_SetChatroomName(string groupid, string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    XzyWxApis.WXSetChatroomName(pointerWxUser, groupid, content.Utf8ToAnsi(), (IntPtr)msgptr1);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 修改群名称
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_ESetChatroomName(string groupid, string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    msgPtr = EUtils.ESetChatroomName(pointerWxUser, groupid, content);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 修改群公告
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_SetChatroomAnnouncement(string groupid, string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    XzyWxApis.WXSetChatroomAnnouncement(pointerWxUser, groupid, content.Utf8ToAnsi(), (IntPtr)msgptr1);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 修改群公告
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_ESetChatroomAnnouncement(string groupid, string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    msgPtr = EUtils.ESetChatroomAnnouncement(pointerWxUser, groupid, content);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch (Exception ex)
                {

                }
            }
            return result;
        }

        /// <summary>
        /// 获取群、好友二维码
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public unsafe string Wx_GetUserQRCode(string userid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetUserQRCode(pointerWxUser, userid, 0, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取群成员资料
        /// </summary>
        /// <param name="groupid"></param>
        /// <returns></returns>
        public unsafe string Wx_GetChatRoomMember(string groupid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetChatRoomMember(pointerWxUser, groupid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 添加群成员
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public unsafe string Wx_AddChatRoomMember(string groupid, string user)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXAddChatRoomMember(pointerWxUser, groupid, user, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        public unsafe string Wx_InviteChatRoomMember(string groupid, string user)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXInviteChatRoomMember(pointerWxUser, groupid, user, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 删除群成员
        /// </summary>
        /// <param name="groupid"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public unsafe string Wx_DeleteChatRoomMember(string groupid, string user)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXDeleteChatRoomMember(pointerWxUser, groupid, user, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }
        #endregion 微信群

        #region 朋友圈
        /// <summary>
        /// 朋友圈评论
        /// </summary>
        /// <param name="snsid"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsComment(string snsid, string content, int replyid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsComment(pointerWxUser, this.wxUser.wxid, snsid, content.Utf8ToAnsi(), replyid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }
        public unsafe string Wx_ESnsComment(string snsid, string content, int replyid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.ESnsComment(pointerWxUser, this.wxUser.wxid, snsid, content, replyid);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 同步朋友圈
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsSync(string key)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsSync(pointerWxUser, key, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 删除评论、点赞
        /// </summary>
        /// <param name="snsid"></param>
        /// <param name="cid"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsObjectOpDeleteComment(string snsid, int cid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsObjectOp(pointerWxUser, snsid, 4, cid, 3, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取朋友圈消息详情
        /// </summary>
        /// <param name="snsid"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsObjectDetail(string snsid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsObjectDetail(pointerWxUser, snsid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                if (datas != null)
                {
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
            }
            return result;
        }

        /// <summary>
        /// 查看指定用户朋友圈
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="snsid">获取到的最后一次的id，第一次调用设置为空</param>
        /// <returns></returns>
        public unsafe string Wx_SnsUserPage(string wxid, string snsid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsUserPage(pointerWxUser, wxid, snsid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                if (datas != null)
                {
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
            }
            return result;
        }

        /// <summary>
        /// 发朋友圈
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="imagelist">图片数组</param>
        /// <returns></returns>
        public unsafe string Wx_SendMoment(string content, List<string> imagelist)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                if (!imagelist.IsNull())
                {
                    string imagestr = "";
                    foreach (string strImage in imagelist)
                    {
                        var reg = new Regex("data:image/(.*);base64,");
                        string fileBase64 = reg.Replace(strImage, "");
                        var reg2 = new Regex("data:video/(.*);base64,");
                        fileBase64 = reg2.Replace(fileBase64, "");
                        string strUploadResult = Wx_SnsUpload(fileBase64);
                        SnsUpload upload = JsonConvert.DeserializeObject<SnsUpload>(strUploadResult);
                        imagestr += String.Format(App.PYQContentImage, upload.big_url, upload.small_url, upload.size, 100, 100);
                    }
                    var result = String.Format(App.PYQContent, wxUser.wxid, content.Utf8ToAnsi(), imagestr);
                    XzyWxApis.WXSendMoments(pointerWxUser, result, (IntPtr)msgptr1);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                    return result;
                }
                else
                {
                    return "参数不正确";
                }
            }
        }

        /// <summary>
        /// 发朋友圈
        /// </summary>
        /// <param name="content">内容</param>
        /// <param name="imagelist">图片数组</param>
        /// <returns></returns>
        public unsafe string Wx_ESendMoment(string content, List<string> imagelist)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                if (!imagelist.IsNull())
                {
                    string result = "";
                    string imagestr = "";
                    foreach (string strImage in imagelist)
                    {
                        var reg = new Regex("data:image/(.*);base64,");
                        string fileBase64 = reg.Replace(strImage, "");
                        var reg2 = new Regex("data:video/(.*);base64,");
                        fileBase64 = reg2.Replace(fileBase64, "");
                        string strUploadResult = Wx_SnsUpload(fileBase64);
                        SnsUpload upload = JsonConvert.DeserializeObject<SnsUpload>(strUploadResult);
                        imagestr += String.Format(App.PYQContentImage, upload.big_url, upload.small_url, upload.size, 100, 100);
                        Thread.Sleep(1000);
                    }
                    var format = String.Format(App.EPYQContent, wxUser.wxid, imagestr);
                    msgPtr = EUtils.ESendSNSImage(pointerWxUser, format, content);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                    return result;
                }
                return "参数错误";
            }
        }

        public unsafe string Wx_ESendLink(string title, string text, string url)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                string result = "";
                var format = String.Format(App.SnsLink, wxUser.wxid, url);
                int ptr = EUtils.ESendSNSLink(pointerWxUser, format, title, text);
                var datas = MarshalNativeToManaged((IntPtr)ptr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref ptr);
                return result;
            }
        }

        /// <summary>
        /// 发送文字朋友圈
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_SendMoment(string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSendMoments(pointerWxUser, content.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return result;
            }
        }

        /// <summary>
        /// 发送文字朋友圈
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public unsafe string Wx_ESendMoment(string content)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.ESendSNS(pointerWxUser, content);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
                return result;
            }
        }

        /// <summary>
        /// 查看朋友圈 ID第一次传空
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsTimeline(string id)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSnsTimeline(pointerWxUser, id, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 朋友圈图片上传
        /// </summary>
        /// <param name="base64"></param>
        /// <returns></returns>
        public unsafe string Wx_SnsUpload(string base64)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    byte[] data = Convert.FromBase64String(base64);
                    if (data.Length > 0)
                    {
                        XzyWxApis.WXSnsUpload(pointerWxUser, data, data.Length, (IntPtr)msgptr1);
                        var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                        result = datas.ConvertToString();
                        Wx_ReleaseEX(ref msgPtr);
                    }
                }
                catch { }
            }
            return result;
        }

        #endregion 朋友圈

        /// <summary>
        /// 获取通讯录
        /// </summary>
        public unsafe void Wx_GetContacts()
        {
            if (wxGroup != null && wxGroup.Count > 0)
            {
                return;
            }
            wxGroup = new List<WxGroup>();
            Dictionary<string, string> dicg = new Dictionary<string, string>();
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                while (true)
                {
                    Thread.Sleep(200);
                    XzyWxApis.WXSyncContact(pointerWxUser, (IntPtr)(msgptr1));
                    if (msgPtr == 0) {
                        continue;
                    }
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    Wx_ReleaseEX(ref msgPtr);
                    if (datas == null) { continue; }
                    var str = datas.ConvertToString();
                    List<Contact> Contact = null;
                    Contact = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Contact>>(str);
                    result = 0;
                    var con = 0;
                    //循环所有通讯录对象，此通讯录包括好友、群、公众号等
                    foreach (var c in Contact)
                    {

                        con = c.Continue;
                        if (con == 0) { break; }
                        if (c.UserName.IsNull())
                        {
                            continue;
                        }
                        if (c.UserName.IndexOf("@chatroom") == -1 && c.UserName.IndexOf("gh_") == -1)
                        {
                            wxContacts.Add(c);
                            SocketModel model = new SocketModel()
                            {
                                action = "getcontact",
                                context = JsonConvert.SerializeObject(c)
                            };
                            WebSocketSend(JsonConvert.SerializeObject(model));
                        }
                        else if (c.UserName.IndexOf("@chatroom") != -1)
                        {
                            wxGroups.Add(c);
                            SocketModel model = new SocketModel()
                            {
                                action = "getgroup",
                                context = JsonConvert.SerializeObject(c)
                            };
                            WebSocketSend(JsonConvert.SerializeObject(model));
                        }
                        else if (c.UserName.IndexOf("gh_") != -1)
                        {
                            wxGzhs.Add(c);
                            SocketModel model = new SocketModel()
                            {
                                action = "getgzh",
                                context = JsonConvert.SerializeObject(c)
                            };
                            WebSocketSend(JsonConvert.SerializeObject(model));
                        }
                    }
                    if (con == 0) { break; }
                }
                XzyWxApis.WXSyncReset(pointerWxUser);
            }
        }

        /// <summary>
        /// 接受好友请求
        /// </summary>
        /// <param name="stranger"></param>
        /// <param name="ticket"></param>
        /// <returns></returns>
        public unsafe string Wx_AcceptUser(string stranger, string ticket)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXAcceptUser(pointerWxUser, stranger, ticket, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 注销
        /// </summary>
        /// <returns></returns>
        public unsafe string Wx_Logout()
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    XzyWxApis.WXLogout(pointerWxUser, (IntPtr)msgptr1);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch { }

            }
            return result;
        }

        /// <summary>
        /// 二维码登录
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public unsafe string Wx_QRCodeLogin(string username, string password)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXQRCodeLogin(pointerWxUser, username, password, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取好友详情
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public unsafe string Wx_GetContact(string wxid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetContact(pointerWxUser, wxid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 设置用户备注
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_SetUserRemark(string wxid, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSetUserRemark(pointerWxUser, wxid, context.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        public unsafe string Wx_ESetUserRemark(string wxid, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.ESetUserRemark(pointerWxUser, wxid, context);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 删除好友
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public unsafe string Wx_DeleteUser(string wxid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXDeleteUser(pointerWxUser, wxid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取登录token
        /// </summary>
        /// <returns></returns>
        public unsafe string Wx_GetLoginToken()
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetLoginToken(pointerWxUser, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 设置微信id
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public unsafe string Wx_SetWeChatID(string wxid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSetWeChatID(pointerWxUser, wxid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取本地二维码信息
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public unsafe string Wx_QRCodeDecode(string path)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXQRCodeDecode(pointerWxUser, path, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取其他设备登陆请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public unsafe string Wx_ExtDeviceLoginGet(string url)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXExtDeviceLoginGet(pointerWxUser, url, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 确认其他设备登陆请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public unsafe string Wx_ExtDeviceLoginOK(string url)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXExtDeviceLoginOK(pointerWxUser, url, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 设置用户资料
        /// </summary>
        /// <param name="nick_name"></param>
        /// <param name="unsigned"></param>
        /// <param name="sex"></param>
        /// <param name="country"></param>
        /// <param name="provincia"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public unsafe string Wx_SetUserInfo(string nick_name, string unsigned, int sex, string country, string provincia, string city)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSetUserInfo(pointerWxUser, nick_name, unsigned, sex, country, provincia, city, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取62数据
        /// </summary>
        /// <returns></returns>
        public unsafe string Wx_GenerateWxDat()
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGenerateWxDat(pointerWxUser, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 断线重连
        /// </summary>
        /// <returns></returns>
        public unsafe string Wx_AutoLogin(string token)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXAutoLogin(pointerWxUser, token, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }


        public unsafe string Wx_GetPeopleNearby(float lat, float lng)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetPeopleNearby(pointerWxUser, lat, lng, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 搜索用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public unsafe string Wx_SearchContact(string user)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSearchContact(pointerWxUser, user, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 添加好友
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <param name="type"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_AddUser(string v1, string v2, int type, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXAddUser(pointerWxUser, v1, v2, type, context.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }
        public unsafe string Wx_EAddUser(string v1, string v2, int type, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.EAddUser(pointerWxUser, v1, v2, type, context);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 附近的人打招呼
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_ESayHello(string v1, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.ESayHello(pointerWxUser, v1, context);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }


        /// <summary>
        /// 公众号搜索
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public unsafe string Wx_WebSearch(string search)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXWebSearch(pointerWxUser, search.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }
        public unsafe string Wx_EWebSearch(string search)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.EWebSearch(pointerWxUser, search);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }


        /// <summary>
        /// 获取公众号菜单
        /// </summary>
        /// <param name="gzhid"></param>
        /// <returns></returns>
        public unsafe string GetSubscriptionInfo(string gzhid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetSubscriptionInfo(pointerWxUser, gzhid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 执行公众号菜单
        /// </summary>
        /// <param name="wxid">公众号用户名gh* 开头的</param>
        /// <param name="uin">通过WXGetSubscriptionInfo获取</param>
        /// <param name="key">通过WXGetSubscriptionInfo获取</param>
        /// <returns></returns>
        public unsafe string Wx_SubscriptionCommand(string wxid, uint uin, string key)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSubscriptionCommand(pointerWxUser, wxid, uin, key, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 阅读链接
        /// </summary>
        /// <param name="url"></param>
        /// <param name="uin"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public unsafe string Wx_RequestUrl(string url, string uin, string key)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXRequestUrl(pointerWxUser, url, key, uin, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 获取访问链接token
        /// </summary>
        /// <param name="ghid"></param>
        /// <param name="url"></param>
        /// <returns></returns>
        public unsafe string Wx_GetRequestToken(string ghid, string url)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetRequestToken(pointerWxUser, ghid, url, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 后期所有标签
        /// </summary>
        /// <returns></returns>
        public unsafe string Wx_GetContactLabelList()
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXGetContactLabelList(pointerWxUser, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 设置用户标签
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="labelid"></param>
        /// <returns></returns>
        public unsafe string Wx_SetContactLabel(string wxid, string labelid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSetContactLabel(pointerWxUser, wxid, labelid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_AddContactLabel(string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXAddContactLabel(pointerWxUser, context.Utf8ToAnsi(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_EAddContactLabel(string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.EAddContactLabel(pointerWxUser, context);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 创建标签
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_DeleteContactLabel(string labelid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXDeleteContactLabel(pointerWxUser, labelid, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 同步收藏
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public unsafe string Wx_FavSync(string key)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXFavSync(pointerWxUser, key, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 添加收藏
        /// </summary>
        /// <param name="fav_object"></param>
        /// <returns></returns>
        public unsafe string Wx_FavAddItem(string fav_object)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXFavAddItem(pointerWxUser, fav_object, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 查看收藏
        /// </summary>
        /// <param name="favid"></param>
        /// <returns></returns>
        public unsafe string Wx_FavGetItem(string favid)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXFavGetItem(pointerWxUser, favid.ConvertToInt32(), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// 发送链接消息
        /// </summary>
        /// <param name="wxid"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public unsafe string Wx_SendAppMsg(string wxid, string context)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXSendAppMsg(pointerWxUser, wxid, Encoding.Default.GetString(Encoding.UTF8.GetBytes(context)), (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        public unsafe string Wx_ESendAppMsg(string wxid, string appid, string sdkver, string title, string des, string url, string thumburl)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                msgPtr = EUtils.ESendAppMsg(pointerWxUser, wxid, appid, sdkver, title, des, url, thumburl);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }

        /// <summary>
        /// token登录
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public unsafe string Wx_LoginRequest(string token, string str62)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                byte[] data62Bytes = Convert.FromBase64String(str62);
                XzyWxApis.WXLoadWxDat(pointerWxUser, data62Bytes, data62Bytes.Length, (IntPtr)msgptr1);
                var data1 = MarshalNativeToManaged((IntPtr)msgPtr);
                result = data1.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);

            }
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXLoginRequest(pointerWxUser, token, (IntPtr)msgptr1);
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                result = datas.ConvertToString();
                Wx_ReleaseEX(ref msgPtr);
            }
            return result;
        }


        /// <summary>
        /// 接受转账
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        public unsafe string Wx_WXTransferOperation(string msg)
        {
            var result = "";
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                try
                {
                    XzyWxApis.WXTransferOperation(pointerWxUser, msg, (IntPtr)msgptr1);
                    var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                    result = datas.ConvertToString();
                    Wx_ReleaseEX(ref msgPtr);
                }
                catch (Exception ex) { }

            }
            return result;
        }


        public int redPack;

        //读红包key
        public unsafe Dictionary<string, packitme> RedpackOK(string json, int Timestamp)
        {
            fixed (int* WxUser1 = &pointerWxUser, msgptr1 = &msgPtr)
            {
                XzyWxApis.WXReceiveRedPacket(pointerWxUser, json, (IntPtr)msgptr1);
                if ((int)msgptr1 == 0) { return null; }
                var fromwxid = "";
                var key = MarshalNativeToManaged((IntPtr)msgPtr).ToString();
                if (key == null)
                {
                    return null;
                }

                Wx_ReleaseEX(ref msgPtr);

                WXReceiveRedPacketJson wxReceiveRedPacketjson = Newtonsoft.Json.JsonConvert.DeserializeObject<WXReceiveRedPacketJson>(key);

                key = wxReceiveRedPacketjson.Key;
                RedPacketJson redPacketjson = Newtonsoft.Json.JsonConvert.DeserializeObject<RedPacketJson>(wxReceiveRedPacketjson.External);
                fromwxid = redPacketjson.SendUserName;

                if (!this.SET_redpack_Key(key, json))
                {
                    return null;
                }
                else
                {
                    /*接收到新红包*/
                    this.CallBackRedPack(false, -2, "收到新红包", key, fromwxid, Timestamp);
                }

                #region 领取红包
                XzyWxApis.WXOpenRedPacket(pointerWxUser, json, key, (IntPtr)msgptr1);
                if ((int)msgptr1 == 0) { return null; }
                var datas22 = MarshalNativeToManaged((IntPtr)msgPtr);
                var str22 = datas22.ToString();
                Wx_ReleaseEX(ref msgPtr);
                #endregion

                #region 循环接收红包事件，先隐藏掉
                double time = Utilities.GetTimestamp;
                //while (true)
                //{
                //    Thread.Sleep(500);

                //读取红包，要在领了红包后再调用此方法查看
                XzyWxApis.WXQueryRedPacket(pointerWxUser, json, 0, (IntPtr)msgptr1);
                if ((int)msgptr1 == 0) { return null; }
                var datas = MarshalNativeToManaged((IntPtr)msgPtr);
                var str = datas.ToString();
                Wx_ReleaseEX(ref msgPtr);

                ReadPackJson redpackjson = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadPackJson>(str);
                Dictionary<string, packitme> ipackitme = new Dictionary<string, packitme>();
                if (redpackjson.External != "")
                {
                    ReadPackItem redpackitem = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadPackItem>(redpackjson.External);
                    if (redpackitem.HeadTitle != null)
                    {
                        var nowcount = redpackitem.RecNum;
                        var count = redpackitem.TotalNum;


                        if (nowcount == count || redpackitem.HeadTitle.IndexOf("被抢光") != -1)
                        {
                            this.CallBackRedPack(false, -1, "红包被抢光,读包中", key, fromwxid, Timestamp);
                            #region  抢光之后开始翻页
                            Bk2:;
                            var countpage = Convert.ToInt32(Convert.ToDouble(count) / 11.00);
                            if (count % 11 > 0)
                            {
                                countpage = countpage + 1;
                            }
                            ipackitme = new Dictionary<string, packitme>();

                            var index = 0;
                            List<packitme> ilist = new List<packitme>();

                            for (int i = 0; i < countpage + countpage; i++)
                            {

                                XzyWxApis.WXQueryRedPacket(pointerWxUser, json, i, (IntPtr)msgptr1);
                                if ((int)msgptr1 == 0) { return null; }
                                var datas1 = MarshalNativeToManaged((IntPtr)msgPtr);
                                var str1 = datas1.ToString();
                                Wx_ReleaseEX(ref msgPtr);

                                var redpackjson1 = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadPackJson>(str1);
                                var redpackitem1 = Newtonsoft.Json.JsonConvert.DeserializeObject<ReadPackItem>(redpackjson1.External);
                                this.CallBackRedPack(false, i + 1, string.Format("读红包第{0}页", i + 1), key, fromwxid, Timestamp);
                                foreach (var rec in redpackitem1.Record)
                                {
                                    packitme packitme = Newtonsoft.Json.JsonConvert.DeserializeObject<packitme>(rec.ToString());

                                    if (!ipackitme.ContainsKey(packitme.UserName))
                                    {
                                        packitme.xh = index;

                                        ipackitme.Add(packitme.UserName, packitme);
                                        index++;
                                        ilist.Add(packitme);
                                    }
                                }

                            }


                            if (index == count)
                            {
                                this.CallBackRedPack(false, 0, "读包完毕", key, fromwxid, Timestamp, ipackitme);
                                return ipackitme;

                            }
                            else
                            {
                                goto Bk2;
                            }
                            #endregion
                        }
                        else
                        {
                            if (Utilities.GetTimestamp - time > 60 * 1000)
                            {
                                this.CallBackRedPack(false, -3, "红包超时", key, fromwxid, Timestamp, ipackitme);
                                return null;
                            }
                        }
                    }
                }

                //}
                #endregion

                return null;
            }
        }

        public Dictionary<string, string> dic_redpack { get; set; }
        public object obj = new object();
        public bool SET_redpack_Key(string key, string json)
        {
            lock (obj)
            {
                try
                {
                    if (dic_redpack == null)
                    {
                        dic_redpack = new Dictionary<string, string>();
                    }

                    if (!dic_redpack.ContainsKey(key))
                    {
                        dic_redpack.Add(key, json);
                        return true;
                    }
                }
                catch
                {

                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 收到新红包时的回调处理
        /// </summary>
        /// <param name="ok"></param>
        /// <param name="page"></param>
        /// <param name="msg"></param>
        /// <param name="key"></param>
        /// <param name="fromuser"></param>
        /// <param name="Timestamp"></param>
        /// <param name="dic"></param>
        public void CallBackRedPack(bool ok, int page, string msg, string key, string fromuser, int Timestamp, Dictionary<string, packitme> dic = null)
        {
            PackMsg packmsg = new PackMsg();
            packmsg.msg = msg;
            packmsg.key = key;
            packmsg.fromuser = fromuser;
            packmsg.Timestamp = Timestamp;
            if (ok)
            {
                packmsg.ok = true;
                packmsg.packitme = dic;
            }
            else
            {
                packmsg.ok = false;
                packmsg.page = page;

            }
        }

        /// <summary>
        /// 释放内存
        /// </summary>
        /// <param name="hande"></param>
        public void Wx_ReleaseEX(ref int hande)
        {
            XzyWxApis.WXRelease((IntPtr)hande);
            hande = 0;
        }

        #endregion
    }
}
