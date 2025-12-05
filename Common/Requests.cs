namespace Common;

public sealed record AuthenticationRequest(string? Username, byte[]? Password);
public sealed record UserCreateRequest(string? Username, byte[]? Password);
public sealed record AuthenticationReply(AuthenticationResult Result, string Reason);
public enum AuthenticationResult 
{
    Success,
    Failure,
    WaitingForApproval,
    Approved,
    Rejected
}