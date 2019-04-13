using WebApi.Model;
using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using System.Drawing;
using WebApi.Utils;
using System.Configuration;
using WebApi.MyWebSocket;
using WebApi.WeChat;
using System.Threading;

namespace WebApi.Controllers
{
    /// <summary>
    /// 用户模块
    /// </summary>
    [RoutePrefix("api/user")]
    [Error]
    public class UserController : ApiController
    {
        /// <summary>
        /// 获取登录二维码，（如果需要消息回调用同样的uuid 创建websocket isreset传false）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("loginscan")]
        public IHttpActionResult UserLoginScan(ScanLoginModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                //new空对象
                MySocket socket = new MySocket();
                XzyWeChatThread xzy = new XzyWeChatThread(socket, model.devicename);
                DicSocket dicSocket = new DicSocket()
                {
                    socket = socket,
                    weChatThread = xzy
                };
                XzyWebSocket._dicSockets.Remove(model.uuid);
                XzyWebSocket._dicSockets.Add(model.uuid, dicSocket);
                while (xzy.ScanQrCode == "")
                {
                    Thread.Sleep(200);
                }
                result.Success = true;
                result.Context = xzy.ScanQrCode;
                return Ok(result);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrContext = ex.Message;
                return Ok(result);
            }
        }

        /// <summary>
        /// 账号密码+62登录，登录后使用相同的UUID链接websocket也可以再次接收消息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("login62")]
        public IHttpActionResult UserLogin62(UserLoginModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                //new空对象
                MySocket socket = new MySocket();
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid) && !model.isreset)
                {
                    XzyWebSocket._dicSockets[model.uuid].socket = socket;
                    //更新微信线程socket，回调消息
                    XzyWebSocket._dicSockets[model.uuid].weChatThread._socket = socket;
                }
                else//否则创建连接池
                {
                    XzyWeChatThread xzy = new XzyWeChatThread(socket, model.username, model.password, model.str62, model.devicename);
                    DicSocket dicSocket = new DicSocket()
                    {
                        socket = socket,
                        weChatThread = xzy
                    };
                    XzyWebSocket._dicSockets.Remove(model.uuid);
                    XzyWebSocket._dicSockets.Add(model.uuid, dicSocket);
                }
                result.Success = true;
                result.Context = "调用成功，如未登陆成功可能账号受限，请使用websocket登陆查看详细原因";
                return Ok(result);
            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrContext = e.Message;
                return Ok(result);
            }
        }

        /// <summary>
        /// 设置wxid
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("setwxid")]
        public IHttpActionResult UserSetWxid(UserSetWxidModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_SetWeChatID(model.wxid);
                    result.Success = true;
                    result.Context = res;
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "不存在该websocket连接";
                    return Ok(result);
                }

            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrContext = e.Message;
                return Ok(result);
            }
        }

        /// <summary>
        /// 设置个人信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("setuserinfo")]
        public IHttpActionResult UserSetUserInfo(UserSetUserInfoModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_SetUserInfo(model.nickname, model.sign, model.sex, model.country, model.provincia, model.city);
                    result.Success = true;
                    result.Context = res;
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "不存在该websocket连接";
                    return Ok(result);
                }

            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrContext = e.Message;
                return Ok(result);
            }
        }

        /// <summary>
        /// 获取个人信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("get")]
        public IHttpActionResult GetInfo(BaseModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {

                    result.Success = true;
                    result.Context = JsonConvert.SerializeObject(XzyWebSocket._dicSockets[model.uuid].weChatThread.userData);
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "不存在该websocket连接";
                    return Ok(result);
                }

            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrContext = e.Message;
                return Ok(result);
            }
        }

        /// <summary>
        /// 微信注销
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("logout")]
        public IHttpActionResult LogOut(BaseModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {

                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_Logout();
                    XzyWebSocket._dicSockets[model.uuid].weChatThread = null;
                    XzyWebSocket._dicSockets.Remove(model.uuid);
                    result.Success = true;
                    result.Context = res;
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "不存在该websocket连接";
                    return Ok(result);
                }

            }
            catch (Exception e)
            {
                result.Success = false;
                result.ErrContext = e.Message;
                return Ok(result);
            }
        }

    }
}
