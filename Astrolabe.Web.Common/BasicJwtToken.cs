namespace Astrolabe.Web.Common;

public record BasicJwtToken(byte[] SecretKey, string Issuer, string Audience);
