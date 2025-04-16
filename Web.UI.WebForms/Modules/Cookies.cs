using System;
using System.Web;

namespace ParadimeWeb.WorkflowGen.WebServer.Modules
{
    public class Cookies : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Context_BeginRequest;
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
