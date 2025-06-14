using System;
using System.Configuration;
using System.Text;
using Advantys.Security;
using Jose;
using System.Web;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class Login : System.Web.UI.Page
    {
        private static readonly string ApplicationUrl = ConfigurationManager.AppSettings["ApplicationUrl"];
        private static readonly string SessionTokenCookie = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"]) ? ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"] : "wfgen_token";
        private static readonly byte[] SessionTokenSigningSecret = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenSigningSecret"]);
        private bool IsAsyncRequest;

        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            IsAsyncRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (Page.IsPostBack && IsAsyncRequest) 
            {
                var QUERY = Request.Form["QUERY"];
                Response.ClearContent();
                Response.Clear();
                Response.Expires = 0;
                Response.BufferOutput = false;
                Response.ContentType = "application/json";

                string error = null;
                var returnUrl = Request["ReturnUrl"] == null ? ApplicationUrl : Request["ReturnUrl"];
                returnUrl = returnUrl.EndsWith("show.aspx?QUERY=CONTEXT&REQUEST_QUERY=WELCOME&NO_REDIR=Y") ? ApplicationUrl : returnUrl;

                if (QUERY == "LOGIN")
                {
                    var username = Request.Form["username"];
                    var password = Request.Form["password"];
                    var MainDbSource = ConfigurationManager.ConnectionStrings["MainDbSource"];
                    var removeDomainPrefix = ConfigurationManager.AppSettings["ApplicationSecurityRemoveDomainPrefix"].Split(',').GetEnumerator();
                    int maxLoginAttempts;
                    if (!int.TryParse(ConfigurationManager.AppSettings["ApplicationSecurityMaxLoginAttempts"], out maxLoginAttempts))
                        maxLoginAttempts = 5;
                    var applicationSecurityPasswordManagementMode = "V5";
                    if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"]))
                        applicationSecurityPasswordManagementMode = ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"];
                    var ID_USER = -1;
                    var AUTH = "N";

                    using (var conn = new SqlConnection(MainDbSource.ConnectionString))
                    using (var cmd = conn.CreateCommand())
                    {
                        conn.Open();
                        cmd.CommandText = "SELECT USERS.ID_USER, DIRECTORY.AUTH FROM USERS, DIRECTORY WHERE USERS.ID_DIRECTORY=DIRECTORY.ID_DIRECTORY AND USERS.USERNAME=@USERNAME";
                        cmd.Parameters.AddWithValue("@USERNAME", username);
                        var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            ID_USER = reader.GetInt32(0);
                            AUTH = reader.GetString(1);
                        }
                    }

                    if (ID_USER < 0)
                    {
                        error = "NOT_FOUND";
                    }
                    else if (AUTH == "Y")
                    {
                        try
                        {
                            new UserIdentity(MainDbSource.ProviderName, MainDbSource.ConnectionString, removeDomainPrefix).Authenticate(username, password, maxLoginAttempts, applicationSecurityPasswordManagementMode);
                            var token = JWT.Encode(username, SessionTokenSigningSecret, JwsAlgorithm.HS256);
                            Response.Cookies.Add(new HttpCookie(SessionTokenCookie, token) { HttpOnly = true, SameSite = SameSiteMode.Lax });
                            Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "WFGEN") { HttpOnly = true, SameSite = SameSiteMode.Lax });
                        }
                        catch (MaxLoginAttemptsReachedException)
                        {
                            error = "MAX_ATTEMPTS";
                        }
                        catch
                        {
                            error = "INVALID_CREDENTIALS";
                        }
                    }
                    else
                    {
                        error = "NO_AUTH";
                    }
                }
                else if (QUERY == "SSO")
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
