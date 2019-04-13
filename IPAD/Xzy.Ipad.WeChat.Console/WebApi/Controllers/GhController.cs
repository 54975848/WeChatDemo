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
    /// 公众号模块
    /// </summary>
    [RoutePrefix("api/gh")]
    [Error]
    public class GhController : ApiController
    {
        /// <summary>
        /// 获取公众号内容
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("get")]
        public IHttpActionResult GhGet(GhModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.GetSubscriptionInfo(model.ghid);
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
        /// 关注公众号
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("follow")]
        public IHttpActionResult GhFlower(GhModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_AddUser(model.ghid, "", 0, "");
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
        /// 搜索公众号
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("search")]
        public IHttpActionResult GhSearch(GhSearchModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_WebSearch(model.name);
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
        /// 执行公众号菜单
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("subscriptioncommand")]
        public IHttpActionResult SubscriptionCommand(GhSubscriptionCommandModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_SubscriptionCommand(model.ghid, uint.Parse(model.uin), model.key);
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
        /// 阅读链接
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("requesturl")]
        public IHttpActionResult RequestUrl(GhRequestUrl model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_RequestUrl(model.url, model.uin, model.key);
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
        /// 获取阅读链接token  （uin,key）
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getrquesttoken")]
        public IHttpActionResult GetRequestToken(GhRequestTokenModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.Wx_GetRequestToken(model.ghid, model.url);
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
