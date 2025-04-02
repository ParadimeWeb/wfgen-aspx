using System;
using System.Configuration;
using System.Security.Principal;
using System.Threading;
using System.Web;

namespace ParadimeWeb.WorkflowGen.WebServer.Modules
{
    public class Hooks : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.AuthenticateRequest += Context_AuthenticateRequest;
        }
        private void Context_AuthenticateRequest(object sender, EventArgs e)
        {
            var application = (HttpApplication)sender;
            var context = application.Context;

            if (context.Request.Headers["Authorization"] == $"Bearer {ConfigurationManager.AppSettings["HooksBearerToken"]}")
            {
                context.Request.Headers.Add("IsWebhook", "Y");
                var windowsIdentity = WindowsIdentity.GetCurrent();
                var genericIdentity = new GenericIdentity(windowsIdentity.Name, windowsIdentity.AuthenticationType);
                var genericPrincipal = new GenericPrincipal(genericIdentity, null);
                Thread.CurrentPrincipal = genericPrincipal;
                HttpContext.Current.User = Thread.CurrentPrincipal;
                return;
            }
        }
    }
}
