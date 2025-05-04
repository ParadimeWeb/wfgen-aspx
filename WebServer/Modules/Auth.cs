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
            application.BeginRequest += Context_BeginRequest;
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

        private void Context_BeginRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;
            var delegatorCookiename = "WFGEN_ID_USER_DELEGATOR";
            var ID_USER_DELEGATOR = context.Request.Form["ID_USER_DELEGATOR"] ?? "-1";
            var delegatorCookie = context.Request.Cookies[delegatorCookiename];
            if (delegatorCookie == null || delegatorCookie.Value != ID_USER_DELEGATOR)
            {
                if (delegatorCookie == null)
                {
                    context.Response.Cookies.Add(new HttpCookie(delegatorCookiename, ID_USER_DELEGATOR)
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Lax
                    });
                }
                else
                {
                    delegatorCookie.Value = ID_USER_DELEGATOR;
                    delegatorCookie.HttpOnly = true;
                    delegatorCookie.SameSite = SameSiteMode.Lax;
                    context.Response.Cookies.Set(delegatorCookie);
                }
            }
        }
    }
}
