using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.Model;

namespace WebApi.Controllers
{
    /// <summary>
    /// 系统模块
    /// </summary>
    [RoutePrefix("api/test")]
    [Error]
    public class TestController : ApiController
    {
        [HttpGet]
        [Route("testonline")]
        public IHttpActionResult TestOnline()
        {
            ApiServerMsg result = new ApiServerMsg();
            result.Success = true;
            result.Context = "服务正常";
            return Ok(result);
        }
    }
}
