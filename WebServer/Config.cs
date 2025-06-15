using System.Collections;
using System.Configuration;
using System.Text;
using System.Web;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    internal static class Config
    {
        internal static readonly string SessionTokenCookie = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"]) ? ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenCookie"] : "wfgen_token";
        internal static readonly byte[] SessionTokenSigningSecret = Encoding.UTF8.GetBytes(ConfigurationManager.AppSettings["ApplicationSecurityAuthSessionTokenSigningSecret"]);
        internal static readonly IEnumerator ApplicationSecurityRemoveDomainPrefix = ConfigurationManager.AppSettings["ApplicationSecurityRemoveDomainPrefix"].Split(',').GetEnumerator();
        internal static readonly string ApplicationSecurityPasswordManagementMode = ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"];
        internal static readonly ConnectionStringSettings MainDbSource = ConfigurationManager.ConnectionStrings["MainDbSource"];
        internal static readonly string ApplicationUrl = VirtualPathUtility.RemoveTrailingSlash(ConfigurationManager.AppSettings["ApplicationUrl"]);
        internal static readonly string ApplicationSecurityMinimumPasswordLength = ConfigurationManager.AppSettings["ApplicationSecurityMinimumPasswordLength"];
        internal static readonly string ApplicationSmtpServer = ConfigurationManager.AppSettings["ApplicationSmtpServer"];
        internal static readonly string ApplicationSmtpPort = ConfigurationManager.AppSettings["ApplicationSmtpPort"];
        internal static readonly string EngineNotificationDefaultSenderName = ConfigurationManager.AppSettings["EngineNotificationDefaultSenderName"];
        internal static readonly string EngineNotificationDefaultSender = ConfigurationManager.AppSettings["EngineNotificationDefaultSender"];
        internal static readonly string ApplicationSecurityMaxLoginAttempts = ConfigurationManager.AppSettings["ApplicationSecurityMaxLoginAttempts"];
    }
}
