using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using Owin;
using Swashbuckle.Application;
using WebApi.MyWebSocket;
using WebApi.Utils;

namespace WebApi
{
    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            //跨域配置
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));
            config.MapHttpAttributeRoutes();
            config.Routes.MapHttpRoute(name: "WeChatApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
            config.EnableSwagger(c =>
            {
                c.SingleApiVersion("v1", "WebAPI");
                c.IncludeXmlComments(GetXmlCommentsPath());
                c.ResolveConflictingActions(x => x.First());

            }).EnableSwaggerUi();

            appBuilder.UseWebApi(config);
            Auth.Init();
            XzyWebSocket.Init();
        }

        private static string GetXmlCommentsPath()
        {
            return $@"{System.AppDomain.CurrentDomain.BaseDirectory}\WebApi.XML";
        }
    }
}