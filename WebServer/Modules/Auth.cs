﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Security.Principal;
using System.Text;
using System.Web;
using Advantys.Security.Http;
using Jose;

namespace ParadimeWeb.WorkflowGen.WebServer.Modules
{
    public class Auth : IHttpModule
    {
        private static readonly string SessionTokenCookie = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"]) ? ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"] : "wfgen_token";
        private static readonly byte[] SessionTokenSigningSecret = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenSigningSecret"]);

        public void Dispose() {}
        public void Init(HttpApplication application)
        {
            var context = application.Context;
            application.BeginRequest += new EventHandler(beginRequest);
            application.AuthenticateRequest += new EventHandler(authenticateRequest);
        }

        private bool skipAuthentication(HttpApplication webApplication)
        {
            if (string.IsNullOrEmpty(webApplication.Request.Headers["authorization"]) && ConfigurationManager.AppSettings["GraphqlApiKeyEnabled"] == "Y" && webApplication.Request.Headers["x-wfgen-graphql-api-key"] == ConfigurationManager.AppSettings["GraphqlApiKey"] && (webApplication.Request.RawUrl.EndsWith("/graphql", StringComparison.InvariantCultureIgnoreCase) || webApplication.Request.RawUrl.EndsWith("/graphql?", StringComparison.InvariantCultureIgnoreCase) || webApplication.Request.RawUrl.EndsWith("/graphql/schema", StringComparison.InvariantCultureIgnoreCase)))
            {
                return true;
            }
            var absolutePath = webApplication.Request.Url.AbsolutePath;
            var anonPages = new List<string>()
            {
              "forgotpassword.aspx",
              "login.aspx"
            };
            foreach (string str in anonPages)
            {
                if (absolutePath.EndsWith(str, StringComparison.InvariantCultureIgnoreCase))
                {
                    return true;
                }
            }
            if (!Path.HasExtension(absolutePath))
            {
                return false;
            }
            return new List<string>()
            {
              ".js",
              ".css",
              ".png",
              ".htm",
              ".gif",
              ".ico",
              ".jpg",
              ".svg"
            }.Contains(Path.GetExtension(absolutePath));
        }

        private void authenticateRequest(object source, EventArgs e)
        {
            var webApplication = (HttpApplication)source;
            if (webApplication.Request.Headers["Authorization"] == $"Bearer {ConfigurationManager.AppSettings["HooksBearerToken"]}")
            {
                webApplication.Request.Headers.Add("IsWebhook", "Y");
                return;
            }
            if (webApplication.Request.Cookies["wfgen_auth_type"]?.Value == "SSO")
            {
                var jwtModule = new JWTAuthenticationModule();
                jwtModule.AuthenticateRequest(source, e);
                return;
            }
            if (skipAuthentication(webApplication))
            {
                return;
            }
            var absolutePath = webApplication.Request.Url.AbsolutePath;
            if (absolutePath.EndsWith("/graphql/dist/server.js", StringComparison.InvariantCultureIgnoreCase))
            {
                var authModule = new AuthenticationModule();
                authModule.AuthenticateRequest(source, e);
                return;
            }
            var sessionToken = webApplication.Request.Cookies[SessionTokenCookie]?.Value;
            if (string.IsNullOrEmpty(sessionToken))
            {
                webApplication.Response.Redirect($"{Config.ApplicationUrl}/login.aspx?ReturnUrl={HttpUtility.UrlEncode(webApplication.Request.Url.PathAndQuery)}");
                return;
            }
            var decodedToken = JWT.Decode(sessionToken, SessionTokenSigningSecret, JwsAlgorithm.HS256);
            webApplication.Context.User = new GenericPrincipal(new GenericIdentity(decodedToken), null);
        } 
        
        private void beginRequest(object source, EventArgs e)
        {
            var application = (HttpApplication)source;
            var delegatorCookiename = "WFGEN_ID_USER_DELEGATOR";
            var ID_USER_DELEGATOR = application.Request.Form["ID_USER_DELEGATOR"] ?? "-1";
            var delegatorCookie = application.Request.Cookies[delegatorCookiename];
            if (delegatorCookie == null || delegatorCookie.Value != ID_USER_DELEGATOR)
            {
                if (delegatorCookie == null)
                {
                    application.Response.Cookies.Add(new HttpCookie(delegatorCookiename, ID_USER_DELEGATOR)
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
                    application.Response.Cookies.Set(delegatorCookie);
                }
            }
        }
    }
}
