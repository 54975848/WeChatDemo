using Fleck;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Web;
using WebApi.WeChat;

namespace WebApi.Model
{
    public class SocketModel
    {
        public string action { get; set; }
        public string context { get; set; }
    }

    public class DicSocket {
        public IWebSocketConnection socket;
        public XzyWeChatThread weChatThread;
        public DateTime dateTime;
    }

    public class MySocket : IWebSocketConnection
    {
        Action IWebSocketConnection.OnOpen { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action IWebSocketConnection.OnClose { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action<string> IWebSocketConnection.OnMessage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action<byte[]> IWebSocketConnection.OnBinary { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action<byte[]> IWebSocketConnection.OnPing { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action<byte[]> IWebSocketConnection.OnPong { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        Action<Exception> IWebSocketConnection.OnError { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IWebSocketConnectionInfo IWebSocketConnection.ConnectionInfo => throw new NotImplementedException();

        bool IWebSocketConnection.IsAvailable => throw new NotImplementedException();

        void IWebSocketConnection.Close()
        {
            throw new NotImplementedException();
        }

        Task IWebSocketConnection.Send(string message)
        {
            throw new NotImplementedException();
        }

        Task IWebSocketConnection.Send(byte[] message)
        {
            throw new NotImplementedException();
        }

        Task IWebSocketConnection.SendPing(byte[] message)
        {
            throw new NotImplementedException();
        }

        Task IWebSocketConnection.SendPong(byte[] message)
        {
            throw new NotImplementedException();
        }
    }
}