namespace ParadimeWeb.WorkflowGen.Data.GraphQL
{
    public class MultipartFileUpload : Variable
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public MultipartFileUpload(string name, string fileName, string contentType, byte[] content) : base(name, null, "Upload")
        {
            FileName = fileName;
            ContentType = contentType;
            Content = content;
        }
    }
}
