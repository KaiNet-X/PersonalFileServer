namespace Common;

public sealed record AuthenticationRequest(string Username, string Password);
public sealed record UserCreateRequest(string Username, string Password);
public sealed record AuthenticationReply(bool Result, string Reason);