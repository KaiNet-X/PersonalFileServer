namespace Common;

public sealed record AuthenticationRequest(string? Username, byte[]? Password);
public sealed record UserCreateRequest(string? Username, byte[]? Password);
public sealed record AuthenticationReply(bool Result, string Reason);