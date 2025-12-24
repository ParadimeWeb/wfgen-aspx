namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model
{
    public class Approval
    {
        public const string Pending = "PENDING";
        public const string Approved = "APPROVED";
        public const string Rejected = "REJECTED";
        public const string Disabled = "DISABLED";
    }
    public enum ApprovalType
    {
        Approved,
        Rejected
    }
}
