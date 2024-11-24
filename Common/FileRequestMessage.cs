namespace Common;

using Net.Messages;

public class FileRequestMessage : MessageBase
{
    private string _fileName;
    private string _directory;

    public FileRequestType RequestType { get; init; }
    public Guid RequestId { get; init; }
    public bool EndOfMessage { get; init; }
    public byte[] FileData { get; set; }
    public string PathRequest { get; init; }

    public string FileName 
    {
        get
        {
            return _fileName ??= Path.GetFileName(PathRequest);
        }
    }
    public string Directory
    {
        get
        {
            return _directory ??= Path.GetDirectoryName(PathRequest);
        }
    }
}

public enum FileRequestType
{
    Download,
    Upload,
    Delete,
    Tree
}