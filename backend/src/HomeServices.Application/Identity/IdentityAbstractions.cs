using HomeServices.Domain.Identity;

namespace HomeServices.Application.Identity;

/// <summary>Sends text messages. Development uses a fake that logs; production uses an SMS gateway (T70).</summary>
public interface ISmsSender
{
    Task SendAsync(PhoneNumber to, string message, CancellationToken cancellationToken);
}

/// <summary>One-way, keyed hash for secrets we must recognise but never store (SMS codes, refresh tokens).</summary>
public interface ISecretHasher
{
    string Hash(string secret);
}

/// <summary>Generates the numeric code sent by SMS.</summary>
public interface IOtpGenerator
{
    string Generate();
}

/// <summary>Issues signed access tokens and random refresh tokens.</summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(User user);

    string CreateRefreshToken();
}

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
