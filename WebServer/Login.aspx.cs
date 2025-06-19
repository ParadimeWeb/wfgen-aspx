using System;
using Advantys.Security;
using Jose;
using System.Web;
using Newtonsoft.Json;
using System.Data.SqlClient;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class Login : System.Web.UI.Page
    {
        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);

            var IsAsyncRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (Page.IsPostBack && IsAsyncRequest) 
            {
                var QUERY = Request.Form["QUERY"];
                Response.ClearContent();
                Response.Clear();
                Response.Expires = 0;
                Response.BufferOutput = false;
                Response.ContentType = "application/json";

                string error = null;
                var returnUrl = Request["ReturnUrl"] == null ? Config.ApplicationUrl : Request["ReturnUrl"];
                returnUrl = returnUrl.EndsWith("show.aspx?QUERY=CONTEXT&REQUEST_QUERY=WELCOME&NO_REDIR=Y") ? Config.ApplicationUrl : returnUrl;

                if (QUERY == "LOGIN")
                {
                    var username = Request.Form["username"];
                    var password = Request.Form["password"];
                    int maxLoginAttempts;
                    if (!int.TryParse(Config.ApplicationSecurityMaxLoginAttempts, out maxLoginAttempts))
                        maxLoginAttempts = 5;
                    var applicationSecurityPasswordManagementMode = "V5";
                    if (!string.IsNullOrEmpty(Config.ApplicationSecurityPasswordManagementMode))
                        applicationSecurityPasswordManagementMode = Config.ApplicationSecurityPasswordManagementMode;
                    var ID_USER = -1;
                    var AUTH = "N";

                    using (var conn = new SqlConnection(Config.MainDbSource.ConnectionString))
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
                            new UserIdentity(Config.MainDbSource.ProviderName, Config.MainDbSource.ConnectionString, Config.ApplicationSecurityRemoveDomainPrefix).Authenticate(username, password, maxLoginAttempts, applicationSecurityPasswordManagementMode);
                            var token = JWT.Encode(username, Config.SessionTokenSigningSecret, JwsAlgorithm.HS256);
                            Response.Cookies.Add(new HttpCookie(Config.SessionTokenCookie, token) { HttpOnly = true, SameSite = SameSiteMode.Lax });
                            Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "WFGEN") { HttpOnly = true, SameSite = SameSiteMode.Lax });
                        }
                        catch (MaxLoginAttemptsReachedException)
                        {
                            error = "MAX_ATTEMPTS";
                        }
                        catch (UnauthorizedAccessException)
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
