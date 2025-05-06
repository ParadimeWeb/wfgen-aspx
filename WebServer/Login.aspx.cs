using System;
using System.Configuration;
using System.Text;
using Advantys.Security;
using Jose;
using System.Web;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class Login : System.Web.UI.Page
    {
        private static readonly string SessionTokenCookie = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"]) ? ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"] : "wfgen_token";
        private static readonly byte[] SessionTokenSigningSecret = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenSigningSecret"]);

        protected System.Web.UI.WebControls.TextBox txtUsername;
        protected System.Web.UI.WebControls.TextBox txtPassword;
        protected System.Web.UI.WebControls.Label errorLabel;

        private void redirect()
        {
            var returnUrl = Request["ReturnUrl"] == null ? string.Empty : Request["ReturnUrl"];
            Response.Redirect(returnUrl.EndsWith("show.aspx?QUERY=CONTEXT&REQUEST_QUERY=WELCOME&NO_REDIR=Y") ? "~/" : returnUrl);
        }

        protected void btnSSO_Click(object Source, EventArgs e)
        {
            Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "SSO") { HttpOnly = true, SameSite = SameSiteMode.Lax });
            redirect();
        }
        protected void btnSubmit_Click(object Source, EventArgs e)
        {
            var username = txtUsername.Text;
            var password = txtPassword.Text;

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
            }
            catch
            {
                errorLabel.Text = "Invalid crendentials";
                return;
            }

            var token = JWT.Encode(username, SessionTokenSigningSecret, JwsAlgorithm.HS256);
            Response.Cookies.Add(new HttpCookie(SessionTokenCookie, token) { HttpOnly = true, SameSite = SameSiteMode.Lax });
            Response.Cookies.Add(new HttpCookie("wfgen_auth_type", "WFGEN") { HttpOnly = true, SameSite = SameSiteMode.Lax });

            redirect();
        }
    }
}
