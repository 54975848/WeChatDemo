using WebApi.Model;
using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using System.Drawing;
using WebApi.Utils;
using System.Configuration;
using WebApi.MyWebSocket;

namespace WebApi.Controllers
{
    /// <summary>
    /// 系统模块
    /// </summary>
    [RoutePrefix("api/system")]
    [Error]
    public class SystemController : ApiController
    {
        /// <summary>
        /// 设置设备登录key
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("setDeviceKey")]
        public IHttpActionResult SysDeviceKey(DeviceKeyModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (ConfigurationManager.AppSettings["AdminPassword"].ConvertToString() == model.password)
                {
                    App.DeviceKey = model.devicekey;
                    result.Success = true;
                    result.Context = "设置成功";
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "管理员密码不正确，请检查webconfig配置";
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
        /// 查询所有在线微信
        /// </summary>
        /// <param name="password">管理员密码,配置在webconfig中</param>
        /// <returns></returns>
        [HttpGet]
        [Route("getallonline")]
        public IHttpActionResult SysGetAllOnline(string password)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (ConfigurationManager.AppSettings["AdminPassword"].ConvertToString() == password)
                {
                    List<OnlineWxModel> onlineWxModels = new List<OnlineWxModel>();
                    foreach (var a in XzyWebSocket._dicSockets)
                    {
                        OnlineWxModel onlineWx = new OnlineWxModel();
                        onlineWx.uuid = a.Key;
                        if (!a.Value.weChatThread.IsNull())
                        {
                            onlineWx.wxid = a.Value.weChatThread.userData.UserName.ConvertToString();
                            onlineWx.nickname = a.Value.weChatThread.userData.NickName.ConvertToString();
                            onlineWx.headimg = a.Value.weChatThread.userData.HeadImg.ConvertToString();
                            onlineWx.contactcount = a.Value.weChatThread.wxContacts.Count.ConvertToString().ConvertToInt32();
                            onlineWx.groupcount = a.Value.weChatThread.wxGroups.Count.ConvertToString().ConvertToInt32();
                            onlineWx.gzhcount = a.Value.weChatThread.wxGzhs.Count.ConvertToString().ConvertToInt32();
                        }
                        onlineWxModels.Add(onlineWx);
                    }
                    result.Success = true;
                    result.Context = JsonConvert.SerializeObject(onlineWxModels);
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "管理员密码不正确，请检查webconfig配置";
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
        /// 注销所有在线微信
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("logoutall")]
        public IHttpActionResult SysLogOutAll(string password)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (ConfigurationManager.AppSettings["AdminPassword"].ConvertToString() == password)
                {
                    foreach (var a in XzyWebSocket._dicSockets)
                    {
                        var res = a.Value.weChatThread.Wx_Logout();
                    }
                    result.Success = true;
                    result.Context = "全部下线完成";
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "管理员密码不正确，请检查webconfig配置";
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
        /// 注销单个进程
        /// </summary>
        /// <param name="uuid"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("logout")]
        public IHttpActionResult SysLogOutAll(string uuid,string password)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (ConfigurationManager.AppSettings["AdminPassword"].ConvertToString() == password)
                {
                    var res = XzyWebSocket._dicSockets[uuid].weChatThread.Wx_Logout();
                    result.Success = true;
                    result.Context = "注销成功";
                    return Ok(result);
                }
                else
                {
                    result.Success = false;
                    result.Context = "管理员密码不正确，请检查webconfig配置";
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
