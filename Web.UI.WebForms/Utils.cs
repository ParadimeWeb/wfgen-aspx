using System;
using System.Configuration;
using System.IO;
using System.Web;
using WorkflowGen.My.HtmlAgilityPack;

namespace ParadimeWeb.WorkflowGen.Web.UI.WebForms
{
    public class Utils
    {
        public static string GetPaddedUsername(string fullUsername)
        {
            bool removeDomainPrefix = false;
            if (fullUsername.IndexOf("\\") < 0)
            {
                return fullUsername;
            }
            var domainPrefixToRemove = ConfigurationManager.AppSettings["ApplicationSecurityRemoveDomainPrefix"].Split(',').GetEnumerator();
            string userName = fullUsername.Substring(fullUsername.IndexOf("\\") + 1);
            string domain = fullUsername.Substring(0, fullUsername.IndexOf("\\"));
            if (domainPrefixToRemove != null)
            {
                string current;
                for (; domainPrefixToRemove.MoveNext() && !removeDomainPrefix; removeDomainPrefix = current.ToLower().Equals("_all") || current.ToUpper().CompareTo(domain.ToUpper()) == 0)
                    current = (string)domainPrefixToRemove.Current;
                if (!removeDomainPrefix)
                    userName = fullUsername;
            }
            return userName;
        }
        public static string GetHtmlPath(HttpContext context)
        {
            return Path.Combine(Path.GetDirectoryName(context.Request.PhysicalPath), "build\\index.html");
        }
        public static string GetFileDownloadUrl(string appUrl, int processId, int relDataId, int dataSetId, string delegatorId)
        {
            var url = $"{VirtualPathUtility.RemoveTrailingSlash(appUrl)}/show.aspx?QUERY=DATASET_FILE_DOWNLOAD&ID_PROCESS={processId}&ID_RELDATA={relDataId}&ID_DATASET={dataSetId}&NUM_VALUE=1&NO_CHARSET=Y&NO_REDIRECTION=Y";
            return delegatorId != null && delegatorId != "-1" ? $"{url}&ID_USER_DELEGATOR={delegatorId}" : url;
        }
        public static HtmlDocument GetFormArchive(int processInstId, int activityInstId, string delegatorId, string htmlPath, Uri uri)
        {
            var absoluteUrl = uri.GetLeftPart(UriPartial.Path);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(File.ReadAllText(htmlPath));

            var htmlBody = htmlDocument.DocumentNode.SelectSingleNode("//body");
            var scriptTag = htmlBody.SelectSingleNode("script");
            var rootDiv = htmlBody.SelectSingleNode("div");
            htmlBody.RemoveAllChildren();

            var form = htmlDocument.CreateElement("form");
            form.SetAttributeValue("method", "get");
            form.SetAttributeValue("action", absoluteUrl);

            var __WFGENACTION = htmlDocument.CreateElement("input");
            __WFGENACTION.SetAttributeValue("type", "hidden");
            __WFGENACTION.SetAttributeValue("name", "__WFGENACTION");
            __WFGENACTION.SetAttributeValue("id", "__WFGENACTION");
            __WFGENACTION.SetAttributeValue("value", "FORM_ARCHIVE");

            var ID_PROCESS_INST = htmlDocument.CreateElement("input");
            ID_PROCESS_INST.SetAttributeValue("type", "hidden");
            ID_PROCESS_INST.SetAttributeValue("name", "ID_PROCESS_INST");
            ID_PROCESS_INST.SetAttributeValue("id", "ID_PROCESS_INST");
            ID_PROCESS_INST.SetAttributeValue("value", processInstId.ToString());

            var ID_ACTIVITY_INST = htmlDocument.CreateElement("input");
            ID_ACTIVITY_INST.SetAttributeValue("type", "hidden");
            ID_ACTIVITY_INST.SetAttributeValue("name", "ID_ACTIVITY_INST");
            ID_ACTIVITY_INST.SetAttributeValue("id", "ID_ACTIVITY_INST");
            ID_ACTIVITY_INST.SetAttributeValue("value", activityInstId.ToString());

            var ID_USER_DELEGATOR = htmlDocument.CreateElement("input");
            ID_USER_DELEGATOR.SetAttributeValue("type", "hidden");
            ID_USER_DELEGATOR.SetAttributeValue("name", "ID_USER_DELEGATOR");
            ID_USER_DELEGATOR.SetAttributeValue("id", "ID_USER_DELEGATOR");
            ID_USER_DELEGATOR.SetAttributeValue("value", delegatorId ?? "-1");

            form.AppendChild(__WFGENACTION);
            form.AppendChild(ID_PROCESS_INST);
            form.AppendChild(ID_ACTIVITY_INST);
            form.AppendChild(ID_USER_DELEGATOR);
            htmlBody.AppendChild(form);
            htmlBody.AppendChild(rootDiv);
            htmlBody.AppendChild(scriptTag);

            return htmlDocument;
        }
    }
}