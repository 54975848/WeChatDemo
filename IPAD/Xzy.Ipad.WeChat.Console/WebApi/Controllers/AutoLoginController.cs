using WebApi.Model;
using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using System.Drawing;
using WebApi.Utils;
using System.Configuration;
using WebApi.MyWebSocket;
using WebApi.Util;

namespace WebApi.Controllers
{
    /// <summary>
    /// 二次登陆模块
    /// </summary>
    [RoutePrefix("api/autologin")]
    [Error]
    public class AutoLoginController : ApiController
    {
        /// <summary>
        /// 获取62数据（未做base64和 hex解码）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("get62")]
        public IHttpActionResult Get62(BaseModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_GenerateWxDat();
                    WxDat wxDat = JsonConvert.DeserializeObject<WxDat>(res);
                    result.Success = true;
                    result.Context = wxDat.data;
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
        /// 获取62数据 ，base62和 hex解码， 数据内容微62xxxxxx
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("get62hex")]
        public IHttpActionResult Get62Hex(BaseModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_GenerateWxDat();
                    WxDat wxDat = JsonConvert.DeserializeObject<WxDat>(res);
                    result.Success = true;
                    result.Context = Convert62.eStrToHex(wxDat.data);
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
