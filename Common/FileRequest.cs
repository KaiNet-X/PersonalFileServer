namespace Common;

public record FileRequest(FileRequestType RequestType, Guid RequestId, string PathRequest);