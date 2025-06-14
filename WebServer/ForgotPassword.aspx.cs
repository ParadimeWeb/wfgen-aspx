using System;
using System.Configuration;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Advantys.My.Security;
using System.Net.Mail;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class ForgotPassword : System.Web.UI.Page
    {
        private static readonly string ApplicationSecurityPasswordManagementMode = ConfigurationManager.AppSettings["ApplicationSecurityPasswordManagementMode"];
        private static readonly string MainDbSourceConnStr = ConfigurationManager.ConnectionStrings["MainDbSource"].ConnectionString;
        private static readonly string ApplicationUrl = VirtualPathUtility.RemoveTrailingSlash(ConfigurationManager.AppSettings["ApplicationUrl"]);

        private void resetPassword()
        {
            var PASSWORD = Request["PASSWORD"];
            var USER_ID = Convert.ToInt32(Request["USER_ID"]);
            var TOKEN = Request["TOKEN"];
            var dateTime = DateTime.MinValue;
            var utcNow = DateTime.UtcNow;
            int ID_USER = -1;

            using (var conn = new SqlConnection(MainDbSourceConnStr)) 
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT ID_USER, DATEADD(hour, 1, RESETPWD_TIME) as TIME_EXPIRE FROM USERS WHERE RESETPWD_TOKEN = @TOKEN AND ID_USER = @USER_ID";
                cmd.Parameters.AddWithValue("@TOKEN", TOKEN);
                cmd.Parameters.AddWithValue("@USER_ID", USER_ID);
                var reader = cmd.ExecuteReader();
                if (reader.Read()) 
                {
                    ID_USER = reader.GetInt32(0);
                    dateTime = reader.GetDateTime(1);
                }
                reader.Close();
                cmd.Parameters.Clear();
                if (ID_USER > 0 && utcNow < dateTime)
                {
                    string salt;
                    string encryptedValue;
                    if (ApplicationSecurityPasswordManagementMode == "OWH" || ApplicationSecurityPasswordManagementMode == "OWH_FIPS")
                    {
                        salt = CryptographyHelper.CreateSalt32Bytes();
                        encryptedValue = ApplicationSecurityPasswordManagementMode == "OWH" ? CryptographyHelper.EncryptSHA256(salt + PASSWORD) : CryptographyHelper.EncryptSHA256FIPS(salt + PASSWORD);
                    }
                    else
                    {
                        salt = "";
                        encryptedValue = CryptographyHelper.MD5Encrypt(PASSWORD);
                    }
                    cmd.CommandText = "UPDATE USERS SET PASSWORD = @PASSWORD, SALT = @SALT, DATE_UPDATE = @DATE_UPDATED, RESETPWD_TOKEN = @RESETPWD_TOKEN, RESETPWD_TIME = @RESETPWD_TIME, CONN_ATTEMPTS = @CONN_ATTEMPTS WHERE ID_USER = @ID_USER";
                    cmd.Parameters.AddWithValue("@PASSWORD", encryptedValue);
                    cmd.Parameters.AddWithValue("@SALT", salt);
                    cmd.Parameters.AddWithValue("@DATE_UPDATED", utcNow);
                    cmd.Parameters.AddWithValue("@RESETPWD_TOKEN", DBNull.Value);
                    cmd.Parameters.AddWithValue("@RESETPWD_TIME", DBNull.Value);
                    cmd.Parameters.AddWithValue("@CONN_ATTEMPTS", 0);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private bool sendRequestReset()
        {
            var RESET_EMAIL = Request["RESET_EMAIL"];
            var utcNow = DateTime.UtcNow;
            var RESETPWD_TOKEN = Guid.NewGuid().ToString();
            var USERNAME = "";
            var FIRSTNAME = "";
            var LASTNAME = "";

            using (var conn = new SqlConnection(MainDbSourceConnStr))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                var ID_USER = -1;
                var ID_DIRECTORY = -1;
                var AUTH = "N";
                

                cmd.CommandText = "SELECT ID_USER, USERNAME, FIRSTNAME, LASTNAME, ID_DIRECTORY FROM USERS WHERE EMAIL = @EMAIL";
                cmd.Parameters.AddWithValue("@EMAIL", RESET_EMAIL);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ID_USER = reader.GetInt32(0);
                    USERNAME = reader.GetString(1);
                    FIRSTNAME = reader.GetString(2);
                    LASTNAME = reader.GetString(3);
                    ID_DIRECTORY = reader.GetInt32(4);
                }
                reader.Close();
                cmd.Parameters.Clear();

                if (ID_DIRECTORY > 0) 
                {
                    cmd.CommandText = "SELECT AUTH FROM DIRECTORY WHERE ID_DIRECTORY = @DIRECTORY_ID";
                    cmd.Parameters.AddWithValue("@DIRECTORY_ID", ID_DIRECTORY);
                    reader = cmd.ExecuteReader();
                    if (reader.Read()) 
                    {
                        AUTH = reader.GetString(0);
                    }
                    reader.Close();
                    cmd.Parameters.Clear();
                }
                
                if (AUTH == "Y")
                {
                    cmd.CommandText = "UPDATE USERS SET RESETPWD_TOKEN = @RESETPWD_TOKEN, RESETPWD_TIME = @RESETPWD_TIME WHERE ID_USER = @ID_USER";
                    cmd.Parameters.AddWithValue("@RESETPWD_TOKEN", RESETPWD_TOKEN);
                    cmd.Parameters.AddWithValue("@RESETPWD_TIME", utcNow);
                    cmd.Parameters.AddWithValue("@ID_USER", ID_USER);
                }
            }

            if (USERNAME == "")
            {
                return false;
            }

            using (var smtp = new SmtpClient(ConfigurationManager.AppSettings["ApplicationSmtpServer"], Convert.ToInt32(ConfigurationManager.AppSettings["ApplicationSmtpPort"])))
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(ConfigurationManager.AppSettings["EngineNotificationDefaultSender"], ConfigurationManager.AppSettings["EngineNotificationDefaultSenderName"]);
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = false;
                mailMessage.To.Add(new MailAddress(RESET_EMAIL, $"{FIRSTNAME} {LASTNAME}"));
                mailMessage.Subject = "WorkflowGen password reset request";
                mailMessage.Body = $@"Hello {FIRSTNAME} ({USERNAME}),

We have received a request to reset your password. Click on the link below to reset your password:

{ApplicationUrl}/forgotpassword.aspx?QUERY=RESET&TOKEN=53d872dd-f155-417d-a8d4-7da0e697d456

If you did not request this password reset, contact your WorkflowGen administrator.";
                smtp.Send(mailMessage);
            }

            return true;
        }

        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);
            var QUERY = Request["QUERY"];
            var IsAsyncRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (Page.IsPostBack && IsAsyncRequest) 
            {
                Response.ClearContent();
                Response.Clear();
                Response.Expires = 0;
                Response.BufferOutput = false;
                Response.ContentType = "application/json";

                string error = null;
                try
                {
                    if (QUERY == "SEND_REQUEST_RESET")
                    {
                        if (!sendRequestReset())
                        {
                            error = "NOT_EXIST";
                        }
                    }
                    else if (QUERY == "CONFIRM_RESET")
                    {
                        resetPassword();
                    }
                }
                catch 
                {
                    error = "SERVER_ERROR";
                }

                Response.Write(JsonConvert.SerializeObject(new { query = QUERY, error }));
                Response.Flush();
                Response.End();
                return;
            }

            if (QUERY == "RESET")
            {
                var TOKEN = Request["TOKEN"];
                var ID_USER = -1;
                var utcNow = DateTime.UtcNow;
                var dateTime = DateTime.MinValue;
                using (var conn = new SqlConnection(MainDbSourceConnStr))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = "SELECT ID_USER, DATEADD(hour, 1, RESETPWD_TIME) as TIME_EXPIRE FROM USERS WHERE RESETPWD_TOKEN = @TOKEN";
                    cmd.Parameters.AddWithValue("@TOKEN", TOKEN);
                    var reader = cmd.ExecuteReader();
                    if (reader.Read()) 
                    {
                        ID_USER = reader.GetInt32(0);
                        dateTime = reader.GetDateTime(1);
                    }
                }
                ClientScript.RegisterHiddenField("TOKEN", TOKEN);
                ClientScript.RegisterHiddenField("ID_USER", ID_USER.ToString());
                ClientScript.RegisterHiddenField("TOKEN_EXPIRED", ID_USER > 0 && utcNow < dateTime ? "N" : "Y");
            }
        }
    }
}
