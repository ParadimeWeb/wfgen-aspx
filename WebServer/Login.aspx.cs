using System;
using System.Configuration;
using System.Text;
using Advantys.Security;
using Jose;
using System.Web;
using Newtonsoft.Json;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class Login : System.Web.UI.Page
    {
        private static readonly string ApplicationUrl = ConfigurationManager.AppSettings["ApplicationUrl"];
        private static readonly string SessionTokenCookie = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"]) ? ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"] : "wfgen_token";
        private static readonly byte[] SessionTokenSigningSecret = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenSigningSecret"]);
        private bool IsAsyncRequest;
        private string wfgenAction;

        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            IsAsyncRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (Page.IsPostBack && IsAsyncRequest) 
            {
                wfgenAction = Request["__WFGENACTION"];
                Response.ClearContent();
                Response.Clear();
                Response.Expires = 0;
                Response.BufferOutput = false;
                Response.ContentType = "application/json";

                string error = null;
                var returnUrl = Request["ReturnUrl"] == null ? ApplicationUrl : Request["ReturnUrl"];
                returnUrl = returnUrl.EndsWith("show.aspx?QUERY=CONTEXT&REQUEST_QUERY=WELCOME&NO_REDIR=Y") ? ApplicationUrl : returnUrl;

                if (wfgenAction == "LOGIN")
                {
                    var username = Request["username"];
                    var password = Request["password"];

                    var connectionString = ConfigurationManager.ConnectionStrings["MainDbSource"].ConnectionString;
                    var providerName = ConfigurationManager.ConnectionStrings["MainDbSource"].ProviderName;
                    var removeDomainPrefix = ConfigurationManager.AppSettings["ApplicationSecurityRemoveDomainPrefix"].Split(',').GetEnumerator();
                    int maxLoginAttempts;
                    if (!int.TryParse(ConfigurationManager.AppSettings["ApplicationSecurityMaxLoginAttempts"], out maxLoginAttempts))
                        maxLoginAttempts = 5;
                    var applicationSecurityPasswordManagementMode = "V5";
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"]))
                        applicationSecurityPasswordManagementMode = ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"];
                    try
                    {
                        new UserIdentity(providerName, connectionString, removeDomainPrefix).Authenticate(username, password, maxLoginAttempts, applicationSecurityPasswordManagementMode);
                        var token = JWT.Encode(username, SessionTokenSigningSecret, JwsAlgorithm.HS256);
                        Response.Cookies.Add(new HttpCookie(SessionTokenCookie, token) { HttpOnly = true, SameSite = SameSiteMode.Lax });
                        Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "WFGEN") { HttpOnly = true, SameSite = SameSiteMode.Lax });
                    }
                    catch
                    {
                        error = "Invalid credentials";
                    }
                }
                else if (wfgenAction == "SSO")
                {
                    Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "SSO") { HttpOnly = true, SameSite = SameSiteMode.Lax });
                }

                Response.Write(JsonConvert.SerializeObject(new { returnUrl, error }));
                Response.Flush();
                Response.End();
                return;
            }
        }
    }
}
