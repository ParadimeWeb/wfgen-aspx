using System;
using System.Configuration;
using System.Web;
using Advantys.Security.Http;

namespace ParadimeWeb.WorkflowGen.WebServer.Modules
{
    public class Auth : IHttpModule
    {
        public void Dispose()
        {
            
        }

        public void Init(HttpApplication application)
        {
            application.AuthenticateRequest += new EventHandler(AuthenticateRequest);
        }

        public void AuthenticateRequest(object source, EventArgs eventArgs)
        {
            var webApplication = (HttpApplication)source;
            var context = webApplication.Context;

            if (context.Request.Headers["Authorization"] == $"Bearer {ConfigurationManager.AppSettings["HooksBearerToken"]}")
            {
                context.Request.Headers.Add("IsWebhook", "Y");
                return;
            }

            if (context.Request.Cookies["SSO"]?.Value == "1")
            {
                var wfgModule = new JWTAuthenticationModule();
                wfgModule.AuthenticateRequest(source, eventArgs);
            }
        }
    }
}
