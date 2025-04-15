using System.Net.Mail;
using ParadimeWeb.WorkflowGen.Data;
using ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms
{
    public static class Mail
    {
        public static void AddFromProcessParticipant(this MailAddressCollection mailAddresses, string name, int processId, int top = 100)
        {
            using (var wfgen = new DataBaseContext(ConnectionStrings.MainDbSource))
            {
                var users = wfgen.GetLocalProcessParticipantUsers<User>(name, processId, null, pageSize: top).Rows;
                foreach (var u in users)
                {
                    if (string.IsNullOrWhiteSpace(u.Email)) continue;
                    mailAddresses.Add(new MailAddress(u.Email, u.CommonName));
                }
            }
        }

        public static void AddFromGroup(this MailAddressCollection mailAddresses, string name, int top = 100)
        {
            using (var wfgen = new DataBaseContext(ConnectionStrings.MainDbSource))
            {
                var users = wfgen.GetGroupUsers<User>(name, null, "Y", "N", pageSize: top).Rows;
                foreach (var u in users)
                {
                    if (string.IsNullOrWhiteSpace(u.Email)) continue;
                    mailAddresses.Add(new MailAddress(u.Email, u.CommonName));
                }
            }
        }
    }
}
