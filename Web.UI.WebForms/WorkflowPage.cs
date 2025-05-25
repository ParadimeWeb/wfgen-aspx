using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.UI;
using System.Web;
using Newtonsoft.Json;
using WorkflowGen.My.Globalization;
using ParadimeWeb.WorkflowGen.Web.UI.WebForms.Model;
using WorkflowGen.My.Data;
using System.Configuration;
using WorkflowGen.My.Web.UI.WebForms;
using WorkflowGen.My.Security;
using ParadimeWeb.WorkflowGen.Data;
using ParadimeWeb.WorkflowGen.Data.GraphQL;
using Newtonsoft.Json.Linq;
using System.IO.Compression;
using System.Net;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms
{
    public abstract class WorkflowPage : Page
    {
        private string wfgenAction;
        private string replyToUrl;
        private string instancePath;
        private HttpCookie delegatorCookie;
        protected DataBaseContext DbCtx {  get; private set; }
        protected bool IsAsyncRequest { get; private set; }
        protected bool IsWebhook { get; private set; }
        protected bool IsFormArchive { get; private set; }
        protected string LangId { get; private set; }
        protected string CurrentWorkflowActionName { get; private set; }
        protected string StoragePath { get; private set; }
        protected string AbsoluteBaseUrl { get; private set; }
        protected string AbsoluteUrl { get; private set; }
        protected string AuthenticatedUser { get; private set; }
        protected TimeZoneInformation UserTimeZoneInfo { get; private set; }
        protected abstract System.Data.DataSet FormData { get; }

        public WorkflowPage()
        {
            IsFormArchive = false;
            DbCtx = new DataBaseContext();
        }
        protected void AddApproval(string role, bool needed = true)
        {
            using (var dt = FormData.CreateApprovalsTable())
            {
                dt.AddApprovalRow(role, needed);
            }
        }
        protected void SetApprovalNeeded(string role, bool needed, bool reset = true)
        {
            using (var dt = FormData.CreateApprovalsTable())
            {
                var r = dt.Rows.Find(role);
                if (reset)
                {
                    r.ResetApprovalRow(needed ? Approval.Pending : Approval.NotNeeded);
                }
                else
                {
                    if (needed)
                    {
                        if (r.IsNull(ApprovalColumn.Approval) || (string)r[ApprovalColumn.Approval] == Approval.NotNeeded)
                        {
                            r[ApprovalColumn.Approval] = Approval.Pending;
                        }
                    }
                    else
                    {
                        r[ApprovalColumn.Approval] = Approval.NotNeeded;
                    }
                }
            }
        }
        protected void ResetApprovals(bool resetApprover = true, bool resetApprovedBy = true)
        {
            using (var dt = FormData.CreateApprovalsTable())
            {
                foreach (DataRow r in dt.Rows)
                {
                    if (resetApprover)
                    {
                        r[ApprovalColumn.ApproverEmail] =
                            r[ApprovalColumn.ApproverEmployeeNumber] =
                            r[ApprovalColumn.ApproverName] =
                            r[ApprovalColumn.ApproverUserName] = DBNull.Value;

                    }
                    if (resetApprovedBy)
                    {
                        r[ApprovalColumn.ActivityInstId] =
                            r[ApprovalColumn.Approved] =
                            r[ApprovalColumn.ApprovedByEmail] =
                            r[ApprovalColumn.ApprovedByEmployeeNumber] =
                            r[ApprovalColumn.ApprovedByName] =
                            r[ApprovalColumn.ApprovedByUserName] = DBNull.Value;

                    }
                    if (r.IsNull(ApprovalColumn.Approval) || (string)r[ApprovalColumn.Approval] != Approval.NotNeeded)
                    {
                        r[ApprovalColumn.Approval] = Approval.Pending;
                    }
                }
            }
        }
        protected void Approve(string role, ApprovalType approval = ApprovalType.Approved, bool setApproverOnlyIfNull = true)
        {
            using (var dt = FormData.CreateApprovalsTable())
            {
                var assignee = FormData.Assignee();
                var currentUser = FormData.CurrentUser();
                var r = dt.Rows.Find(role);
                r.SetApprovalRow(
                    approval,
                    DateTime.Now,
                    assignee.UserName,
                    assignee.EmployeeNumber,
                    assignee.Email,
                    assignee.CommonName,
                    assignee.Directory,
                    currentUser.UserName,
                    currentUser.EmployeeNumber,
                    currentUser.Email,
                    currentUser.CommonName,
                    currentUser.Directory,
                    FormData.ProcessInstanceId(),
                    FormData.ActivityInstanceId(),
                    setApproverOnlyIfNull);
            }
        }
        protected void FillFormData()
        {
            using (var sw = new StreamReader(Request.Files["FormData"].InputStream, Encoding.UTF8))
            {
                FormData.InitializeTable1();
                FormData.ReadXml(instancePath);
                dynamic jsonData = JsonConvert.DeserializeObject(sw.ReadToEnd());
                foreach (var table in jsonData)
                {
                    string tableName = table.Name;
                    var dt = FormData.Tables[tableName];
                    dt.Rows.Clear();
                    foreach (var row in table.Value)
                    {
                        var newRow = dt.NewRow();
                        foreach (var prop in row)
                        {
                            string column = prop.Name;
                            object value = prop.Value.Value;
                            newRow.SetParam(column, value);
                        }
                        dt.Rows.Add(newRow);
                    }
                }
            }
        }
        protected override void InitializeCulture()
        {
            if (Request["WFGEN_LANG"] == null)
            {
                var langCookie = Request.Cookies["USER_LANG"];
                if (langCookie == null || string.IsNullOrEmpty(langCookie.Value))
                {
                    LangId = "en-US";
                }
                else
                {
                    LangId = langCookie.Value;
                }
            }
            else
            {
                var langCookie = new HttpCookie("USER_LANG")
                {
                    HttpOnly = true
                };
                try
                {
                    CultureInfo.GetCultureInfo(Request["WFGEN_LANG"]);
                    langCookie.Value = Request["WFGEN_LANG"];
                }
                catch
                {
                    langCookie.Value = "en-US";
                }
                LangId = langCookie.Value;
                Response.Cookies.Add(langCookie);
            }

            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(LangId);
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(LangId);
            }
            catch
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            }
        }
        protected override void OnPreLoad(EventArgs e)
        {
            base.OnPreLoad(e);
            AbsoluteUrl = Request.Url.GetLeftPart(UriPartial.Path);
            AbsoluteBaseUrl = AbsoluteUrl.Remove(AbsoluteUrl.LastIndexOf(Request.Url.Segments.Last()));
            IsWebhook = Request.Headers["IsWebhook"] == "Y";
            if (IsWebhook)
            {
                Action<string> run;
                var a = Request["a"];
                var hooks = new Dictionary<string, Action<string>>();
                OnWebhooks(hooks);
                if (!hooks.TryGetValue(a, out run))
                {
                    run = (action) => Response.Write(JsonConvert.SerializeObject(new { error = $"Could not find action {action}" }));
                }
                runAction(run, a);
                return;
            }
            IsAsyncRequest = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            wfgenAction = Request.Form["__WFGENACTION"];
            AuthenticatedUser = Utils.GetPaddedUsername(User.Identity.Name);
            delegatorCookie = Request.Cookies["WFGEN_ID_USER_DELEGATOR"];

            if (wfgenAction == "FORM_ARCHIVE")
            {
                IsFormArchive = true;
                var appUrl = ConfigurationManager.AppSettings["ApplicationUrl"];
                var processInstId = Convert.ToInt32(Request.Form["ID_PROCESS_INST"]);
                var activityInstId = Convert.ToInt32(Request.Form["ID_ACTIVITY_INST"]);
                var delegatorId = Request.Form["ID_USER_DELEGATOR"];
                using (var comm = DbCtx.Connection.CreateCommand())
                {
                    DbCtx.EnsureConnectionIsOpen();
                    comm.CommandText = @"SELECT 
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    AIRD.ID_DATASET
FROM 
    WFRELDATA RD
    JOIN WFACTIVITY_INST_RELDATA AIRD ON AIRD.ID_RELDATA = RD.ID_RELDATA
    JOIN WFACTIVITY_INST AI ON AI.ID_PROCESS_INST = AIRD.ID_PROCESS_INST AND AI.ID_ACTIVITY_INST = AIRD.ID_ACTIVITY_INST
WHERE 
    RD.NAME = 'FORM_ARCHIVE'
    AND AI.ID_STATE = 'closed' AND AI.ID_SUBSTATE = 'completed'
    AND AIRD.ID_PROCESS_INST = @processInstId AND AIRD.ID_ACTIVITY_INST = @activityInstId;
SELECT 
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    PIRD.ID_DATASET
FROM 
    WFRELDATA RD
    JOIN WFPROCESS_INST_RELDATA PIRD ON PIRD.ID_RELDATA = RD.ID_RELDATA
WHERE 
    RD.NAME = 'FORM_ARCHIVE'
    AND ID_PROCESS_INST = @processInstId;";
                    comm.Parameters.AddWithValue("@processInstId", processInstId);
                    comm.Parameters.AddWithValue("@activityInstId", activityInstId);
                    using (var r = comm.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            Response.Redirect(Utils.GetFileDownloadUrl(appUrl, r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), delegatorId), true);
                            return;
                        }
                        else
                        {
                            if (r.NextResult() && r.Read())
                            {
                                Response.Redirect(Utils.GetFileDownloadUrl(appUrl, r.GetInt32(0), r.GetInt32(1), r.GetInt32(2), delegatorId), true);
                                return;
                            }
                        }
                    }
                }
                throw new Exception($"Could not get form archive for request {processInstId}-{activityInstId}");
            }
            if (wfgenAction == "ASYNC_FORM_ARCHIVE")
            {
                IsFormArchive = true;
                using (var comm = DbCtx.Connection.CreateCommand())
                {
                    DbCtx.EnsureConnectionIsOpen();
                    comm.CommandText = @"SELECT 
	u.LANGUAGE,
	u.ID_TIMEZONE,
	(SELECT VALUE FROM USERS_PREFS WHERE ID_USER = u.ID_USER AND NAME = 'LANGUAGE_PREF') AS LANGUAGE_PREF,
	(SELECT VALUE FROM USERS_PREFS WHERE ID_USER = u.ID_USER AND NAME = 'ID_TIMEZONE_PREF') AS ID_TIMEZONE_PREF
FROM 
	USERS u 
WHERE 
	ACTIVE = 'Y' AND USERNAME = @USERNAME";
                    comm.Parameters.AddWithValue("@USERNAME", AuthenticatedUser);
                    using (var r = comm.ExecuteReader())
                    {
                        if (r.Read())
                        {
                            LangId = r.IsDBNull(2) ? r.GetString(0) : r.GetString(2);
                            UserTimeZoneInfo = TimeZoneInformation.GetTimeZone(r.IsDBNull(3) ? r.GetInt32(1) : Convert.ToInt32(r.GetString(3))) ?? TimeZoneInformation.GetTimeZone(r.GetInt32(1));
                        }
                    }
                }

                runAction(() =>
                {
                    if (UserTimeZoneInfo == null)
                    {
                        Response.StatusCode = 511;
                        Response.StatusDescription = "Unauthorized";
                        Response.Write(JsonConvert.SerializeObject(new
                        {
                            error = new
                            {
                                title = "Unauthorized access.",
                                message = $"Unauthorized access with current user {AuthenticatedUser}."
                            }
                        }));
                    }
                    else
                    {
                        OnAsyncFormArchive(Convert.ToInt32(Request.Form["ID_PROCESS_INST"]), Convert.ToInt32(Request.Form["ID_ACTIVITY_INST"]), delegatorCookie?.Value, Request.Form["DATA_NAME"] ?? "FORM_DATA");
                    }
                });
                return;
            }

            string userTimeZone;
            string userTimeZoneInfo;
            var applicationDataPath = ConfigurationManager.AppSettings["ApplicationDataPath"];
            var encryptionKey = ConfigurationManager.AppSettings["ApplicationSecurityEncryptionKey"];
            var stateManager = new StateManager(false, ViewState);

            if (Page.IsPostBack)
            {
                instancePath = (string)stateManager["WFGEN_INSTANCE_PATH"];
                StoragePath = (string)stateManager["WFGEN_STORAGE_PATH"];
                replyToUrl = (string)stateManager["WFGEN_REPLY_TO"];
                userTimeZone = (string)stateManager["WFGEN_USER_TZ"];
                userTimeZoneInfo = (string)stateManager["WFGEN_USER_TZ_INFO"];

                instancePath = applicationDataPath + instancePath.Replace("/", "\\");
                StoragePath = applicationDataPath + StoragePath.Replace("/", "\\");
                var contextPath = Path.Combine(StoragePath, "context.xml");

                if (!File.Exists(instancePath) || !File.Exists(contextPath))
                {
                    var error = new Exception($"WorkflowGen is out of context. The files no longer exist at path {StoragePath}");
                    if (IsAsyncRequest)
                    {
                        Elmah.ErrorSignal.FromCurrentContext().Raise(error);
                        runAction(() =>
                        {
                            Response.StatusCode = 500;
                            Response.StatusDescription = "Internal Server Error";
                            Response.Write(JsonConvert.SerializeObject(new
                            {
                                error =  "errorWorkflowGenOutOfContext"
                            }));
                        });
                        return;
                    }
                    throw error;
                }

                using (var wfgenCtx = new ContextParameters(File.ReadAllText(contextPath)))
                {
                    using (var cmd = DbCtx.Connection.CreateCommand())
                    {
                        var instanceCreated = File.GetCreationTimeUtc(instancePath);
                        DbCtx.EnsureConnectionIsOpen();
                        cmd.CommandText = "SELECT ID_STATE, DATE_START FROM WFACTIVITY_INST WHERE ID_PROCESS_INST = @ProcessInstanceId AND ID_ACTIVITY_INST = @ActivityInstanceId";
                        cmd.Parameters.AddWithValue("@ActivityInstanceId", wfgenCtx.ActivityInstanceId);
                        cmd.Parameters.AddWithValue("@ProcessInstanceId", wfgenCtx.ProcessInstanceId);
                        using (var r = cmd.ExecuteReader())
                        {
                            r.Read();
                            var status = r.GetString(0);
                            var startedAt = r.GetDateTime(1);
                            if (status != "open" || startedAt.Subtract(instanceCreated).TotalSeconds > 0.9)
                            {
                                var error = new Exception($"WorkflowGen is out of context. The files are dirty at path {StoragePath}");
                                if (IsAsyncRequest)
                                {
                                    Elmah.ErrorSignal.FromCurrentContext().Raise(error);
                                    runAction(() =>
                                    {
                                        Response.StatusCode = 500;
                                        Response.StatusDescription = "Internal Server Error";
                                        Response.Write(JsonConvert.SerializeObject(new
                                        {
                                            error = "errorWorkflowGenOutOfContext"
                                        }));
                                    });
                                    return;
                                }
                                throw error;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(userTimeZoneInfo))
                    {
                        UserTimeZoneInfo = new TimeZoneInformation(userTimeZoneInfo);
                    }

                    CurrentWorkflowActionName = wfgenCtx.Contains("CURRENT_ACTION") ? (string)wfgenCtx["CURRENT_ACTION"].Value : "";

                    if (wfgenAction == "POST_DOWNLOAD")
                    {
                        runAction(OnDownload, wfgenAction, wfgenCtx);
                        return;
                    }
                    if (IsAsyncRequest)
                    {
                        Action<string, ContextParameters> run;
                        var actions = new Dictionary<string, Action<string, ContextParameters>>()
                        {
                            { "ASYNC_INIT", OnAsyncInit },
                            { "ASYNC_UPLOAD", OnAsyncUpload },
                            { "ASYNC_SAVE", OnAsyncSave },
                            { "ASYNC_SUBMIT", OnAsyncSubmit },
                            { "ASYNC_MISSING_KEY", OnAsyncMissingTranslation },
                            { "ASYNC_ThrowException", (action, ctx) => throw new Exception("Runtime error in an AJAX call") },
                            { "ASYNC_GetLocalProcessParticipantUsers", OnAsyncGetLocalProcessParticipantUsers },
                            { "ASYNC_GetUsers", OnAsyncGetUsers },
                            { "ASYNC_GetGroupUsers", OnAsyncGetGroupUsers },
                            { "ASYNC_GetUser", OnAsyncGetUser },
                            { "ASYNC_GetManager", OnAsyncGetManager },
                            { "ASYNC_GetGlobalListItems", OnAsyncGetGlobalListItems }
                        };
                        OnAsyncActions(actions);
                        if (!actions.TryGetValue(wfgenAction, out run))
                        {
                            run = (string action, ContextParameters ctx) =>
                            {
                                Response.StatusCode = 501;
                                Response.StatusDescription = "Not Implemented";
                                Response.Write(JsonConvert.SerializeObject(new { error = "errorWorkflowGenActionNotImplemented", parameters = new string[] { action } }));
                            };
                        }
                        runAction(run, wfgenAction, wfgenCtx);
                    }
                }
                return;
            }

            if (!string.IsNullOrEmpty(Request.Form["WFGEN_REPLY_TO"]) && !string.IsNullOrEmpty(Request.Form["WFGEN_STORAGE_PATH"]) && !string.IsNullOrEmpty(Request.Form["WFGEN_INSTANCE_PATH"]))
            {
                instancePath = CryptographyHelper.Decode(Request.Form["WFGEN_INSTANCE_PATH"], encryptionKey);
                stateManager.Add("WFGEN_INSTANCE_PATH", instancePath);
                StoragePath = CryptographyHelper.Decode(Request.Form["WFGEN_STORAGE_PATH"], encryptionKey);
                stateManager.Add("WFGEN_STORAGE_PATH", StoragePath);
                replyToUrl = CryptographyHelper.Decode(Request.Form["WFGEN_REPLY_TO"], encryptionKey);
                stateManager.Add("WFGEN_REPLY_TO", replyToUrl);

                if (!string.IsNullOrEmpty(Request.Form["WFGEN_USER_TZ"]))
                {
                    if (!Regex.IsMatch(Request.Form["WFGEN_USER_TZ"], "^[-|+][0-1][0-9]:[0|3|4][0|5]$"))
                    {
                        userTimeZone = "+00:00";
                    }
                    else
                    {
                        userTimeZone = Request.Form["WFGEN_USER_TZ"];
                    }
                    stateManager.Add("WFGEN_USER_TZ", userTimeZone);
                }
                if (!string.IsNullOrEmpty(Request.Form["WFGEN_USER_TZ_INFO"]))
                {
                    userTimeZoneInfo = CryptographyHelper.Decode(Request.Form["WFGEN_USER_TZ_INFO"], encryptionKey);
                    stateManager.Add("WFGEN_USER_TZ_INFO", userTimeZoneInfo);
                }
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            if (FormData != null)
            {
                FormData.Dispose();
            }
            DbCtx.Dispose();
        }
        protected virtual KeyValuePair<string, Type>[] OnGetUserExtendedAttributes()
        {
            return new KeyValuePair<string, Type>[0];
        }
        protected virtual void OnFormDataInit() { }
        protected virtual void OnWebhooks(Dictionary<string, Action<string>> hooks) { }
        protected virtual void OnAsyncActions(Dictionary<string, Action<string, ContextParameters>> actions) { }
        private List<string> setArchiveCommands(string _commands)
        {
            var commands = _commands.Split(',');
            var approvals = commands.FirstOrDefault(c => c.StartsWith("APPROVALS"));
            var comments = commands.FirstOrDefault(c => c.StartsWith("COMMENTS"));
            var print = commands.FirstOrDefault(c => c.StartsWith("PRINT"));
            var archiveCommands = new List<string>();
            if (approvals != null) archiveCommands.Add(approvals);
            if (comments != null) archiveCommands.Add(comments);
            if (print != null) archiveCommands.Add(print);
            return archiveCommands;
        }
        protected virtual void OnPreAsyncFormArchive(int processInstId, int activityInstId, string delegatorId, string dataName)
        {
            using (var ctx = new DataBaseContext())
            using (var comm = ctx.Connection.CreateCommand())
            {
                ctx.EnsureConnectionIsOpen();
                FormData.GetFormData(ctx, processInstId, activityInstId, dataName, OnGetUserExtendedAttributes());
                var appUrl = ConfigurationManager.AppSettings["ApplicationUrl"];
                comm.CommandText = @"SELECT 
	[PARAM],
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    AIRD.ID_DATASET,
    VALUE_FILE_NAME
FROM 
    WFRELDATA RD
    JOIN WFACTIVITY_PARAM AP ON RD.ID_RELDATA = AP.ID_RELDATA AND RD.ID_PROCESS = AP.ID_PROCESS
    JOIN WFACTIVITY_INST_RELDATA AIRD ON AIRD.ID_RELDATA = RD.ID_RELDATA
    JOIN WFACTIVITY_INST AI ON AI.ID_PROCESS_INST = AIRD.ID_PROCESS_INST AND AI.ID_ACTIVITY_INST = AIRD.ID_ACTIVITY_INST
    JOIN WFDATASET_VALUE DSV ON DSV.ID_DATASET = AIRD.ID_DATASET
WHERE 
    ID_DATATYPE = 'FILE' AND VALUE_FILE_CONTENT IS NOT NULL
    AND AI.ID_STATE = 'closed' AND AI.ID_SUBSTATE = 'completed'
    AND AIRD.ID_PROCESS_INST = @processInstId AND AIRD.ID_ACTIVITY_INST = @activityInstId
GROUP BY
    [PARAM],
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    AIRD.ID_DATASET,
    VALUE_FILE_NAME;
SELECT 
    [PARAM],
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    PIRD.ID_DATASET,
    VALUE_FILE_NAME
FROM 
    WFRELDATA RD
    JOIN WFACTIVITY_PARAM AP ON RD.ID_RELDATA = AP.ID_RELDATA AND RD.ID_PROCESS = AP.ID_PROCESS
    JOIN WFPROCESS_INST_RELDATA PIRD ON PIRD.ID_RELDATA = RD.ID_RELDATA
    JOIN WFDATASET_VALUE DSV ON DSV.ID_DATASET = PIRD.ID_DATASET
WHERE 
    ID_DATATYPE = 'FILE' AND VALUE_FILE_CONTENT IS NOT NULL
    AND ID_PROCESS_INST = @processInstId
GROUP BY
	[PARAM],
    RD.ID_PROCESS,
    RD.ID_RELDATA,
    PIRD.ID_DATASET,
    VALUE_FILE_NAME;";
                comm.Parameters.AddWithValue("@processInstId", processInstId);
                comm.Parameters.AddWithValue("@activityInstId", activityInstId);
                using (var r = comm.ExecuteReader())
                {
                    var completedFiles = new HashSet<string>();
                    do
                    {
                        while (r.Read())
                        {
                            var fileParamName = r.GetString(0);
                            if (!FormData.HasParam(fileParamName) || !completedFiles.Add(fileParamName)) continue;

                            var queryString = HttpUtility.ParseQueryString(string.Empty);
                            queryString.Add("Key", fileParamName);
                            queryString.Add("Path", Utils.GetFileDownloadUrl(appUrl, r.GetInt32(1), r.GetInt32(2), r.GetInt32(3), delegatorId));
                            queryString.Add("Name", r.GetString(4));
                            FormData.SetParam(fileParamName, queryString.ToString());
                        }
                    }
                    while (r.NextResult());
                }
                var archiveCommands = setArchiveCommands(FormData.Commands());
                archiveCommands.Insert(0, "ARCHIVE");
                FormData.SetCommands(string.Join(",", archiveCommands));
                var archiveFarCommands = setArchiveCommands(FormData.FarCommands());
                archiveFarCommands.Insert(0, "ARCHIVE_COPY_LINK");
                archiveFarCommands.Insert(0, "ARCHIVE_DOWNLOAD");
                FormData.SetFarCommands(string.Join(",", archiveFarCommands));
                FormData.SetMoreCommands(string.Join(",", setArchiveCommands(FormData.MoreCommands())));
            }
        }
        protected virtual void OnAsyncFormArchive(int processInstId, int activityInstId, string delegatorId, string dataName)
        {
            OnPreAsyncFormArchive(processInstId, activityInstId, delegatorId, dataName);
            Response.Write(FormData.GetInitData(LangId, UserTimeZoneInfo));
        }
        protected virtual void OnPreAsyncInit(string action, ContextParameters ctx)
        {
            var currentUserExtendedAttributes = OnGetUserExtendedAttributes();
            var allFields = FormData.Tables[TableNames.Table1].Columns.OfType<DataColumn>().Select(c => c.ColumnName).ToList();
            FormData.Initialize(currentUserExtendedAttributes);
            FormData.ReadXml(instancePath);
            FormData.InitializeParams(
                ctx.ProcessInstanceId,
                ctx.ActivityInstanceId,
                AbsoluteUrl,
                Request.Form["version"],
                currentUserExtendedAttributes.Select(i => i.Key).ToArray());
            //
            // handle files
            //
            ctx.ApplyFilter(typeof(ContextFileReference), ContextParameters.ComparisonOperators.Equals);
            foreach (var fileParam in ctx)
            {
                if (fileParam.Direction == ContextParameter.Directions.Out || !FormData.Tables[TableNames.Table1].Columns.Contains(fileParam.Name)) continue;
                var fileCtx = fileParam.Value as ContextFileReference;
                var queryString = HttpUtility.ParseQueryString(string.Empty);
                if (string.IsNullOrEmpty(fileCtx.Path))
                {
                    queryString.Add("Key", fileParam.Name);
                    FormData.SetParam(fileParam.Name, queryString.ToString());
                    continue;
                }

                var fieldValue = FormData.GetParam(fileParam.Name);
                var isZipMode = fileCtx.OriginalPath.IndexOf($"\\zip\\{fileParam.Name}\\") > 0;
                var originalFilePath = Path.Combine(StoragePath, Path.GetFileName(fileCtx.Path));
                var fileDirectory = Path.Combine(StoragePath, "file", fileParam.Name);
                if (isZipMode)
                {
                    using (var zipFile = ZipFile.OpenRead(originalFilePath))
                    {
                        if (File.Exists(originalFilePath))
                        {
                            Directory.CreateDirectory(fileDirectory);
                            zipFile.ExtractToDirectory(fileDirectory);
                        }
                        for (int i = 0; i < zipFile.Entries.Count; i++)
                        {
                            var entry = zipFile.Entries[i];
                            queryString.Add("Key", $"Zip{i}");
                            queryString.Add("Path", Path.Combine("file", fileParam.Name, entry.Name));
                            queryString.Add("Name", entry.Name);
                        }
                    }
                    File.Delete(originalFilePath);
                    FormData.SetParam(fileParam.Name, queryString.ToString());
                    continue;
                }
                if (File.Exists(originalFilePath))
                {
                    Directory.CreateDirectory(fileDirectory);
                    File.Move(originalFilePath, Path.Combine(fileDirectory, fileCtx.Name));
                }
                queryString.Add("Key", fileParam.Name);
                queryString.Add("Path", Path.Combine("file", fileParam.Name, fileCtx.Name));
                queryString.Add("Name", fileCtx.Name);
                FormData.SetParam(fileParam.Name, queryString.ToString());
            }

            OnFormDataInit();

            allFields.Add("SUBMIT_COMMENTS");
            allFields.Add("CANCEL_COMMENTS");
            allFields.Add("APPROVE_COMMENTS");
            allFields.Add("REJECT_COMMENTS");
            var separator = new char[] { ',', ';' };
            FormData.SetRequiredFields(string.Join(",", filterFields(allFields, FormData.RequiredFields().Split(separator, StringSplitOptions.RemoveEmptyEntries))));

            FormData.WriteXml(instancePath, XmlWriteMode.WriteSchema);
        }
        protected virtual void OnAsyncInit(string action, ContextParameters ctx)
        {
            OnPreAsyncInit(action, ctx);
            Response.Write(FormData.GetInitData(LangId, UserTimeZoneInfo));
        }
        protected virtual void OnPreSubmit(string action) {}
        protected virtual void OnAsyncSubmit(string action, ContextParameters ctx)
        {
            FillFormData();
            //
            // handle files
            //
            ctx.ApplyFilter(typeof(ContextFileReference), ContextParameters.ComparisonOperators.Equals);
            foreach (var fileParam in ctx)
            {
                if (fileParam.Direction == ContextParameter.Directions.In || !FormData.Tables[TableNames.Table1].Columns.Contains(fileParam.Name)) continue;
                var fieldVal = FormData.GetParam(fileParam.Name);
                if (fieldVal == null) continue;
                var queryString = HttpUtility.ParseQueryString((string)fieldVal);
                var paths = queryString["Path"]?.Split(',') ?? new string[0];
                var noErrorPaths = paths.Where(p => !p.StartsWith("__Error"));
                var filePath = noErrorPaths.FirstOrDefault();
                if (filePath == null) continue;

                FormData.SetZipFileParam(fileParam.Name, null);
                if (noErrorPaths.Count() > 1)
                {
                    //
                    // Zip file
                    //
                    FormData.SetZipFileParam(fileParam.Name, fieldVal);
                    var zipFileFieldValue = Path.Combine("zip", fileParam.Name, $"{fileParam.Name}.zip");
                    Directory.CreateDirectory(Path.Combine(StoragePath, "zip", fileParam.Name));
                    using (var zipFile = ZipFile.Open(Path.Combine(StoragePath, zipFileFieldValue), ZipArchiveMode.Create))
                    {
                        var names = queryString["Name"].Split(',');
                        for (int i = 0; i < paths.Length; i++)
                        {
                            if (paths[i].StartsWith("__Error")) continue;
                            zipFile.CreateEntryFromFile(Path.Combine(StoragePath, paths[i]), names[i]);
                        }
                        FormData.SetParam(fileParam.Name, zipFileFieldValue);
                    }
                    continue;
                }
                FormData.SetParam(fileParam.Name, filePath);
            }

            FormData.SetFormAction(action.Remove(0, 6));
            FormData.SetFormArchiveFileName("form_archive.htm");

            OnPreSubmit(action);

            FormData.SetConfigurationParam(ConfigurationColumn.Modified, DateTime.Now);
            FormData.WriteXml(instancePath, XmlWriteMode.WriteSchema);
            var htmlDocument = Utils.GetFormArchive(FormData.ProcessInstanceId(), FormData.ActivityInstanceId(), delegatorCookie?.Value, Utils.GetHtmlPath(Context), Request.Url);
            htmlDocument.Save(Path.Combine(StoragePath, FormData.FormArchiveFileName()), Encoding.UTF8);
            Response.Write(JsonConvert.SerializeObject(new { replyTo = replyToUrl }));
        }
        protected virtual void OnDownload(string action, ContextParameters ctx)
        {
            var filePath = Request.Form["FILE_PATH"];
            var dispositionType = Request.Form["FILE_DISPOSITION_TYPE"];

            if (dispositionType != "attachment")
            {
                dispositionType = "inline";
            }
            filePath = Path.Combine(StoragePath, filePath);
            var fileName = Path.GetFileName(filePath);
            Response.ContentType = MimeMapping.GetMimeMapping(fileName);
            ContentDispositionHeaderValue disposition = new ContentDispositionHeaderValue(dispositionType);
            disposition.FileNameStar = fileName;
            Response.AddHeader("Content-Disposition", disposition.ToString());
            Response.AddHeader("Accept-Ranges", "bytes");
            Response.TransmitFile(filePath);
        }
        protected virtual object OnPreAsyncUpload(string action, ContextParameters ctx)
        {
            if (Request.Files.Count < 1)
            {
                return new { Error = "No file" };
            }
            var field = Request.Form["field"];
            var Key = Request.Form["key"];
            var file = Request.Files[0];
            var uploadPath = Path.Combine(StoragePath, "upload", field);
            var fileName = Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadPath, fileName);
            if (filePath.Length > 259)
            {
                return new
                {
                    Error = "File name, {{name}}, is too long. Try renaming it.",
                    Name = fileName
                };
            }
            Directory.CreateDirectory(uploadPath);
            // SaveAs overwrites the file if it exists
            file.SaveAs(filePath);

            return new
            {
                Key,
                Path = $"upload\\{field}\\{fileName}",
                Name = fileName
            };
        }
        protected virtual void OnAsyncUpload(string action, ContextParameters ctx)
        {
            Response.Write(JsonConvert.SerializeObject(OnPreAsyncUpload(action, ctx)));
        }
        protected virtual void OnBeforeAsyncSave() { }
        protected virtual void OnAsyncSave(string action, ContextParameters ctx)
        {
            FillFormData();
            FormData.SetFormArchiveFileName("form_archive.htm");
            OnBeforeAsyncSave();
            FormData.SetConfigurationParam(ConfigurationColumn.Modified, DateTime.Now);

            var activityId = FormData.ActivityId();
            var processInstanceId = FormData.ProcessInstanceId();
            var activityInstanceId = FormData.ActivityInstanceId();
            var deleteDataset = new List<int>();
            var parameters = new List<string>();
            var variables = new List<Variable>();
            var saveFormData = new List<string>();
            var saveFormArchive = new List<string>();
            //
            // handle parameters
            //
            using (var cmd = DbCtx.Connection.CreateCommand())
            {
                DbCtx.EnsureConnectionIsOpen();
                cmd.CommandText = @"SELECT 
	[PARAM],
	[NAME],
    [ID_DATATYPE],
    [DSV].ID_DATASET
FROM 
	[WFACTIVITY_PARAM] [AP]
	JOIN [WFRELDATA] [RD] ON [RD].[ID_RELDATA] = [AP].[ID_RELDATA]
    JOIN [WFPROCESS_INST_RELDATA] [PIRD] ON [PIRD].ID_RELDATA = [RD].[ID_RELDATA]
	LEFT JOIN [WFDATASET_VALUE] [DSV] ON [DSV].ID_DATASET = [PIRD].ID_DATASET
WHERE 
    [ID_ACTIVITY] = @ActivityId
    AND [AP].[DIRECTION] != 'IN'
    AND [ID_PROCESS_INST] = @ProcessInstanceId";
                cmd.Parameters.AddWithValue("@ActivityId", activityId);
                cmd.Parameters.AddWithValue("@ProcessInstanceId", processInstanceId);
                var row = 0;
                var r = cmd.ExecuteReader();
                var variableIndex = 0;
                string variableName = "";
                while (r.Read())
                {
                    row = 0;
                    variableIndex++;
                    variableName = $"variable{variableIndex}";
                    var paramName = r.GetString(0);
                    var dataName = r.GetString(1);
                    if (paramName == "FORM_DATA")
                    {
                        saveFormData.AddRange(new string[] { variableName, dataName });
                        continue;
                    }
                    if (paramName == Table1Column.FormArchive)
                    {
                        saveFormArchive.AddRange(new string[] { variableName, dataName });
                        continue;
                    }

                    var tableName = TableNames.Table1;
                    //
                    // NewDataSet/__Approvals[1]/approved
                    var paramNameSplit = paramName.Split('/');
                    if (paramNameSplit.Length > 2)
                    {
                        var tableNameSplit = paramNameSplit[1].Split(new char[] { '[', ']' });
                        tableName = tableNameSplit[0];
                        if (tableNameSplit.Length > 1)
                        {
                            row = Convert.ToInt32(tableNameSplit[1]) - 1;
                        }
                        paramName = paramNameSplit[2];
                    }
                    if (!FormData.Tables[tableName].Columns.Contains(paramName)) continue;
                    var dataType = r.GetString(2);
                    var dataSetId = r.IsDBNull(3) ? (int?)null : r.GetInt32(3);
                    var paramValue = FormData.GetParam(tableName, paramName, row);
                    if (paramValue == null)
                    {
                        if (dataSetId.HasValue)
                        {
                            deleteDataset.Add(dataSetId.Value);
                        }
                        continue;
                    }

                    if (dataType == "FILE")
                    {
                        var queryString = HttpUtility.ParseQueryString((string)paramValue);
                        var key = queryString["Key"];
                        if (key == null) continue;
                        var paths = queryString["Path"]?.Split(',') ?? new string[0];
                        var noErrorPaths = paths.Where(p => !p.StartsWith("__Error"));
                        var filePath = noErrorPaths.FirstOrDefault();
                        if (filePath == null)
                        {
                            if (dataSetId.HasValue)
                            {
                                deleteDataset.Add(dataSetId.Value);
                            }
                            continue;
                        }

                        var fileSysPath = Path.Combine(StoragePath, filePath);
                        FormData.SetZipFileParam(paramName, null);
                        if (noErrorPaths.Count() > 1)
                        {
                            //
                            // Zip file
                            //
                            var names = queryString["Name"].Split(',');
                            FormData.SetZipFileParam(paramName, paramValue);
                            filePath = Path.Combine("zip", paramName, $"{paramName}.zip");
                            Directory.CreateDirectory(Path.Combine(StoragePath, "zip", paramName));
                            if (File.Exists(fileSysPath))
                            {
                                File.Delete(fileSysPath);
                            }
                            using (var zipFile = ZipFile.Open(fileSysPath, ZipArchiveMode.Create))
                            {
                                for (int i = 0; i < paths.Length; i++)
                                {
                                    if (paths[i].StartsWith("__Error")) continue;
                                    zipFile.CreateEntryFromFile(Path.Combine(StoragePath, paths[i]), names[i]);
                                }
                            }
                        }
                        FormData.SetParam(paramName, filePath);

                        var fileName = Path.GetFileName(filePath);
                        using (var fs = new FileStream(fileSysPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            var uri = new Uri(fs.Name);
                            variables.Add(new Variable(variableName, JObject.FromObject(new
                            {
                                name = fileName,
                                contentType = MimeMapping.GetMimeMapping(fileName),
                                size = fs.Length,
                                updatedAt = DateTime.UtcNow.ToString("O"),
                                url = uri.AbsoluteUri
                            }), "FileInput"));
                            parameters.Add($"{{ name: \"{dataName}\", fileValue: ${variableName} }}");
                        }
                        continue;
                    }
                    if (dataType == "NUMERIC")
                    {
                        variables.Add(new Variable(variableName, JToken.FromObject(paramValue), "Float"));
                        parameters.Add($"{{ name: \"{dataName}\", numericValue: ${variableName} }}");
                        continue;
                    }
                    if (dataType == "DATETIME")
                    {
                        variables.Add(new Variable(variableName, ((DateTime)paramValue).ToUniversalTime().ToString("O"), "DateTime"));
                        parameters.Add($"{{ name: \"{dataName}\", dateTimeValue: ${variableName} }}");
                        continue;
                    }
                    variables.Add(new Variable(variableName, JToken.FromObject(paramValue), "String"));
                    parameters.Add($"{{ name: \"{dataName}\", textValue: ${variableName} }}");
                }
                r.Close();

                if (deleteDataset.Count > 0)
                {
                    cmd.CommandText = $"DELETE FROM [WFDATASET_VALUE] WHERE [ID_DATASET] IN ({string.Join(", ", deleteDataset)})";
                    cmd.Parameters.Clear();
                    cmd.ExecuteNonQuery();
                }
            }

            FormData.WriteXml(instancePath, XmlWriteMode.WriteSchema);

            if (saveFormData.Count > 0) 
            {
                var fileName = Path.GetFileName(instancePath);
                using (var fs = new FileStream(instancePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var uri = new Uri(fs.Name);
                    variables.Add(new Variable(saveFormData[0], JObject.FromObject(new
                    {
                        name = fileName,
                        contentType = "application/xml",
                        size = fs.Length,
                        updatedAt = DateTime.UtcNow.ToString("O"),
                        url = uri.AbsoluteUri
                    }), "FileInput"));
                    parameters.Add($"{{ name: \"{saveFormData[1]}\", fileValue: ${saveFormData[0]} }}");
                }
            }
            if (saveFormArchive.Count > 0) 
            {
                var fileName = FormData.FormArchiveFileName();
                var formArchive = Utils.GetFormArchive(processInstanceId, activityInstanceId, delegatorCookie?.Value, Utils.GetHtmlPath(Context), Request.Url);
                var fileFullName = Path.Combine(StoragePath, fileName);
                formArchive.Save(fileFullName, Encoding.UTF8);
                using (var fs = new FileStream(fileFullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var uri = new Uri(fs.Name);
                    variables.Add(new Variable(saveFormArchive[0], JObject.FromObject(new
                    {
                        name = fileName,
                        contentType = "text/html",
                        size = fs.Length,
                        updatedAt = DateTime.UtcNow.ToString("O"),
                        url = uri.AbsoluteUri
                    }), "FileInput"));
                    parameters.Add($"{{ name: \"{saveFormArchive[1]}\", fileValue: ${saveFormArchive[0]} }}");
                }
            }

            var query = $@"updateRequestDataset(input: {{ 
    number: {processInstanceId}, 
    parameters: [{string.Join(", ", parameters)}] 
}}) {{ clientMutationId }}";

            Client.CreateClient(Client.DefaultUrl, new NetworkCredential(ConfigurationManager.AppSettings["MyGraphQLUsername"], ConfigurationManager.AppSettings["MyGraphQLPassword"]));
            try
            {
                Client.Mutation(query, variables.ToArray());
            }
            catch (Exception ex) 
            {
                throw new Exception($"{User.Identity.Name}", ex);
            }
            

            Response.Write("{ \"replyTo\": \"Success\" }");
        }
        protected virtual void OnAsyncGetLocalProcessParticipantUsers(string action, ContextParameters ctx) =>
            Response.Write(JsonConvert.SerializeObject(Request["pid"] != null ?
                Model.User.GetLocalProcessParticipantUsers(
                    Request["n"],
                    Convert.ToInt32(Request["pid"]),
                    Request["q"],
                    string.IsNullOrEmpty(Request["ea"]) ? new string[0] : Request["ea"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    string.IsNullOrEmpty(Request["p"]) ? 1 : Convert.ToInt32(Request["p"]),
                    string.IsNullOrEmpty(Request["ps"]) ? 20 : Convert.ToInt32(Request["ps"])) :
                Model.User.GetLocalProcessParticipantUsers(
                    Request["n"],
                    Request["pn"],
                    Request["q"],
                    string.IsNullOrEmpty(Request["ea"]) ? new string[0] : Request["ea"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    string.IsNullOrEmpty(Request["p"]) ? 1 : Convert.ToInt32(Request["p"]),
                    string.IsNullOrEmpty(Request["ps"]) ? 20 : Convert.ToInt32(Request["ps"]))));
        protected virtual void OnAsyncGetUsers(string action, ContextParameters ctx) =>
            Response.Write(JsonConvert.SerializeObject(Model.User.GetUsers(
                Request["query"],
                Request["active"],
                Request["archive"],
                Request["directory"],
                string.IsNullOrEmpty(Request["extraAttributes"]) ? new string[0] : Request["extraAttributes"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                string.IsNullOrEmpty(Request["page"]) ? 1 : Convert.ToInt32(Request["page"]),
                string.IsNullOrEmpty(Request["pageSize"]) ? 20 : Convert.ToInt32(Request["pageSize"]))));
        protected virtual void OnAsyncGetGroupUsers(string action, ContextParameters ctx) =>
            Response.Write(JsonConvert.SerializeObject(Model.User.GetGroupUsers(
                Request["n"],
                Request["q"],
                Request["active"],
                Request["archive"],
                Request["dir"],
                string.IsNullOrEmpty(Request["ea"]) ? new string[0] : Request["ea"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                string.IsNullOrEmpty(Request["p"]) ? 1 : Convert.ToInt32(Request["p"]),
                string.IsNullOrEmpty(Request["ps"]) ? 20 : Convert.ToInt32(Request["ps"]))));
        protected virtual void OnAsyncGetUser(string action, ContextParameters ctx) =>
            Response.Write(JsonConvert.SerializeObject(Model.User.GetUser(Request["u"], Request["id"], string.IsNullOrEmpty(Request["ea"]) ? new string[0] : Request["ea"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))));
        protected virtual void OnAsyncGetManager(string action, ContextParameters ctx) =>
            Response.Write(JsonConvert.SerializeObject(Model.User.GetManager(Request["u"], string.IsNullOrEmpty(Request["ea"]) ? new string[0] : Request["ea"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))));
        protected virtual void OnAsyncGetGlobalListItems(string action, ContextParameters ctx)
        {
            QueryResult<Dictionary<string, object>> result;
            using (var db = new DataBaseContext())
            {
                result = db.GetGlobalListItems(
                    Request["listName"],
                    string.IsNullOrEmpty(Request["columns"]) ? new string[] { "value", "text" } : Request["columns"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
                    Request["sort"],
                    string.IsNullOrEmpty(Request["asc"]) ? true : Convert.ToBoolean(Request["asc"]),
                    Request["filterCol"],
                    Request["filterComp"],
                    Request["filterVal"],
                    string.IsNullOrEmpty(Request["filterLang"]) ? "default" : Request["filterLang"],
                    string.IsNullOrEmpty(Request["p"]) ? 1 : Convert.ToInt32(Request["p"]),
                    string.IsNullOrEmpty(Request["ps"]) ? 20 : Convert.ToInt32(Request["ps"]));
            }
            Response.Write(JsonConvert.SerializeObject(result));
        }
        protected virtual void OnAsyncMissingTranslation(string action, ContextParameters ctx) 
        {
            using (var streamReader = new StreamReader(Request.Files["MissingTranslation"].InputStream))
            {
                var filePath = Server.MapPath("~/i18n/en/translation_missing.json");
                var json1 = JObject.Parse(streamReader.ReadToEnd());
                var json2 = JObject.Parse(File.ReadAllText(filePath));

                json1.Merge(json2, new JsonMergeSettings { MergeArrayHandling = MergeArrayHandling.Union });
                File.WriteAllText(filePath, json1.ToString());
            }

            Response.Write(JsonConvert.SerializeObject(new { Result = "OK" }));
        }
        private void runAction(Action<string, ContextParameters> run, string action = null, ContextParameters ctx = null)
        {
            Response.ClearContent();
            Response.Clear();
            Response.Expires = 0;
            Response.BufferOutput = false;
            Response.ContentType = "application/json";
            run(action, ctx);
            Response.Flush();
            Response.End();
        }
        private void runAction(Action run) => runAction((action, ctx) => run());
        private void runAction(Action<string> run, string action = null) => runAction((_, ctx) => run(action));
        private List<string> filterFields(List<string> allFields, string[] rules)
        {
            var filtered = new List<string>();
            foreach (var r in rules)
            {
                var rule = r.Trim().Replace(".", "\\.");
                if (rule == "*")
                {
                    filtered = new List<string>(allFields);
                    continue;
                }
                if (rule == "^*")
                {
                    filtered = new List<string>();
                    continue;
                }

                string regexPattern;
                Regex regex;
                if (rule.StartsWith("^"))
                {
                    if (filtered.Count == 0) continue;

                    regexPattern = rule.EndsWith("*") ? rule.Remove(rule.Length - 1, 1).Replace("*", "[a-zA-Z\\._0-9]*") : $"{rule.Replace("*", "[a-zA-Z\\._0-9]*")}$";
                    regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                    filtered.RemoveAll(new Predicate<string>(regex.IsMatch));
                    continue;
                }

                regexPattern = rule.EndsWith("*") ? $"^{rule.Remove(rule.Length - 1, 1).Replace("*", "[a-zA-Z\\._0-9]*")}" : $"^{rule.Replace("*", "[a-zA-Z\\._0-9]*")}$";
                regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
                foreach (var field in allFields.FindAll(new Predicate<string>(regex.IsMatch)))
                {
                    if (!filtered.Contains(field))
                    {
                        filtered.Add(field);
                    }
                }
            }
            return filtered;
        }
    }
}
