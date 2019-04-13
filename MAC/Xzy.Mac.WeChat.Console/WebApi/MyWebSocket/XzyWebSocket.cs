using Fleck;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApi.Model;
using WebApi.Utils;
using WebApi.WeChat;

namespace WebApi.MyWebSocket
{
    public class XzyWebSocket
    {
        /// <summary>
        /// websocket 连接池
        /// </summary>
        public static Dictionary<string, DicSocket> _dicSockets = new Dictionary<string, DicSocket>();

        /// <summary>
        /// 初始化socket服务
        /// </summary>
        public static void Init() {
            var server = new WebSocketServer($"ws://0.0.0.0:{ConfigurationManager.AppSettings["WebSocketHost"]}");
            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    string url = "";
                    NameValueCollection queryString;
                    MyUtils.ParseUrl(socket.ConnectionInfo.Path,out url, out queryString);
                    string action= queryString["action"];
                    string uuid = queryString["uuid"];
                    string devicename = queryString["devicename"];
                    string isreset = queryString["isreset"];
                    if (action == "scan")//扫码登录
                    {
                        //如果连接池包含 则更新socket
                        if (_dicSockets.ContainsKey(uuid) && isreset == "false")
                        {
                            _dicSockets[uuid].socket = socket;
                            //更新微信线程socket，回调消息
                            _dicSockets[uuid].weChatThread._socket = socket;
                            _dicSockets[uuid].weChatThread.SocketIsConnect = true;
                        }
                        else//否则创建连接池
                        {
                            XzyWeChatThread xzy = new XzyWeChatThread(socket, devicename);
                            DicSocket dicSocket = new DicSocket()
                            {
                                socket = socket,
                                weChatThread = xzy,
                                dateTime = DateTime.Now
                            };
                            _dicSockets.Remove(uuid);
                            _dicSockets.Add(uuid, dicSocket);
                            xzy.SocketIsConnect = true;
                        }
                    }
                    else if (action == "62")
                    {//62登录
                        string username = queryString["username"];
                        string password = queryString["password"];
                        string str62 = queryString["str62"];
                        //如果连接池包含 则更新socket
                        if (_dicSockets.ContainsKey(uuid) && isreset == "false")
                        {
                            _dicSockets[uuid].socket = socket;
                            //更新微信线程socket，回调消息
                            _dicSockets[uuid].weChatThread._socket = socket;
                            _dicSockets[uuid].weChatThread.SocketIsConnect = true;
                        }
                        else//否则创建连接池
                        {
                            XzyWeChatThread xzy = new XzyWeChatThread(socket, username, password, str62,devicename);
                            DicSocket dicSocket = new DicSocket()
                            {
                                socket = socket,
                                weChatThread = xzy,
                                dateTime = DateTime.Now
                            };
                            _dicSockets.Remove(uuid);
                            _dicSockets.Add(uuid, dicSocket);
                            xzy.SocketIsConnect = true;
                        }
                    }
                };
                socket.OnClose = () =>
                {
                    try
                    {
                        _dicSockets.Where(p => p.Value.socket == socket).ToList().FirstOrDefault().Value.weChatThread.SocketIsConnect = false;
                    }
                    catch (Exception ex) { }
                    Console.WriteLine("连接断开!");
                };
                socket.OnMessage = message =>
                {
                    //Console.WriteLine(message);
                    //allSockets.ToList().ForEach(s => s.Send("Echo: " + message));
                };
            });
        }
    }
}
