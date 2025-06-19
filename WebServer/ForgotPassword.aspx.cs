using System;
using System.Text;
using Newtonsoft.Json;
using System.Data.SqlClient;
using Advantys.My.Security;
using System.Net.Mail;

namespace ParadimeWeb.WorkflowGen.WebServer
{
    public partial class ForgotPassword : System.Web.UI.Page
    {
        private string resetPassword()
        {
            var PASSWORD = Request.Form["PASSWORD"];
            var USER_ID = Convert.ToInt32(Request.Form["USER_ID"]);
            var TOKEN = Request.Form["TOKEN"];
            var dateTime = DateTime.MinValue;
            var utcNow = DateTime.UtcNow;
            var ID_USER = -1;
            var ApplicationSecurityMinimumPasswordLength = Convert.ToInt32(Config.ApplicationSecurityMinimumPasswordLength);

            if (PASSWORD.Length < ApplicationSecurityMinimumPasswordLength)
            {
                return "MIN_LENGTH";
            }

            using (var conn = new SqlConnection(Config.MainDbSource.ConnectionString)) 
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
                    if (Config.ApplicationSecurityPasswordManagementMode == "OWH" || Config.ApplicationSecurityPasswordManagementMode == "OWH_FIPS")
                    {
                        salt = CryptographyHelper.CreateSalt32Bytes();
                        encryptedValue = Config.ApplicationSecurityPasswordManagementMode == "OWH" ? CryptographyHelper.EncryptSHA256(salt + PASSWORD) : CryptographyHelper.EncryptSHA256FIPS(salt + PASSWORD);
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
                    cmd.Parameters.AddWithValue("@ID_USER", ID_USER);
                    cmd.ExecuteNonQuery();
                }
            }

            return null;
        }

        private string sendRequestReset()
        {
            var RESET_EMAIL = Request.Form["RESET_EMAIL"];
            var utcNow = DateTime.UtcNow;
            var RESETPWD_TOKEN = Guid.NewGuid().ToString();
            var USERNAME = "";
            var EMAIL = "";
            var FIRSTNAME = "";
            var LASTNAME = "";
            var AUTH = "N";
            var ID_USER = -1;

            using (var conn = new SqlConnection(Config.MainDbSource.ConnectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT USERS.ID_USER, USERS.EMAIL, USERS.USERNAME, USERS.FIRSTNAME, USERS.LASTNAME, DIRECTORY.AUTH FROM USERS, DIRECTORY WHERE USERS.ID_DIRECTORY=DIRECTORY.ID_DIRECTORY AND USERNAME=@EMAIL AND EMAIL IS NOT NULL";
                cmd.Parameters.AddWithValue("@EMAIL", RESET_EMAIL);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    ID_USER = reader.GetInt32(0);
                    EMAIL = reader.GetString(1);
                    USERNAME = reader.GetString(2);
                    FIRSTNAME = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    LASTNAME = reader.GetString(4);
                    AUTH = reader.GetString(5);
                }
                if (ID_USER < 0)
                {
                    return "NOT_FOUND";
                }
                if (AUTH != "Y") 
                {
                    return "NO_AUTH";
                }
                reader.Close();
                cmd.Parameters.Clear();
                cmd.CommandText = "UPDATE USERS SET RESETPWD_TOKEN = @RESETPWD_TOKEN, RESETPWD_TIME = @RESETPWD_TIME WHERE ID_USER = @ID_USER";
                cmd.Parameters.AddWithValue("@RESETPWD_TOKEN", RESETPWD_TOKEN);
                cmd.Parameters.AddWithValue("@RESETPWD_TIME", utcNow);
                cmd.Parameters.AddWithValue("@ID_USER", ID_USER);
                cmd.ExecuteNonQuery();
            }

            using (var smtp = new SmtpClient(Config.ApplicationSmtpServer, Convert.ToInt32(Config.ApplicationSmtpPort)))
            using (var mailMessage = new MailMessage())
            {
                mailMessage.From = new MailAddress(Config.EngineNotificationDefaultSender, Config.EngineNotificationDefaultSenderName);
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.SubjectEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = false;
                mailMessage.To.Add(new MailAddress(EMAIL, $"{FIRSTNAME} {LASTNAME}"));
                mailMessage.Subject = "WorkflowGen password reset request";
                mailMessage.Body = $@"Hello {FIRSTNAME} ({USERNAME}),

We have received a request to reset your password. Click on the link below to reset your password:

{Config.ApplicationUrl}/forgotpassword.aspx?QUERY=RESET&TOKEN={RESETPWD_TOKEN}

If you did not request this password reset, contact your WorkflowGen administrator.";
                smtp.Send(mailMessage);
            }

            return null;
        }

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
                if (QUERY == "SEND_REQUEST_RESET")
                {
                    error = sendRequestReset();
                }
                else if (QUERY == "CONFIRM_RESET")
                {
                    error = resetPassword();
                }

                Response.Write(JsonConvert.SerializeObject(new { query = QUERY, error }));
                Response.Flush();
                Response.End();
                return;
            }

            if (Request.Params["QUERY"] == "RESET")
            {
                var TOKEN = Request.Params["TOKEN"];
                var ID_USER = -1;
                var utcNow = DateTime.UtcNow;
                var dateTime = DateTime.MinValue;
                using (var conn = new SqlConnection(Config.MainDbSource.ConnectionString))
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
