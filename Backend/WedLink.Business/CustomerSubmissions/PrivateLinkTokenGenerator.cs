using System.Security.Cryptography;

namespace WedLink.Business.CustomerSubmissions;

public sealed class PrivateLinkTokenGenerator : IPrivateLinkTokenGenerator
{
    public (string Token, string Hash) Generate()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        return (token, Hash(token));
    }

    public string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
