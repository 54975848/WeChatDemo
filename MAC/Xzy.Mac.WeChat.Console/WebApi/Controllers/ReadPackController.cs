using WebApi.Model;
using System;
using System.Collections.Generic;
using System.Web.Http;
using Newtonsoft.Json;
using System.Drawing;
using WebApi.Utils;
using System.Configuration;
using WebApi.MyWebSocket;
using System.Linq;

namespace WebApi.Controllers
{
    /// <summary>
    /// 红包模块
    /// </summary>
    [RoutePrefix("api/readpack")]
    [Error]
    public class ReadPackController : ApiController
    {
        /// <summary>
        /// 抢红包
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("getpack")]
        public IHttpActionResult GetPack(MessageModel model)
        {
            ApiServerMsg result = new ApiServerMsg();
            try
            {
                if (XzyWebSocket._dicSockets.ContainsKey(model.uuid))
                {
                    var res = XzyWebSocket._dicSockets[model.uuid].weChatThread.RedpackOK(JsonConvert.SerializeObject(model.msg), model.msg.Timestamp);
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
