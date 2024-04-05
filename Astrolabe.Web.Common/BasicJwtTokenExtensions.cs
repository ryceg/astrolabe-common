using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Astrolabe.Web.Common;

public delegate string TokenGenerator(IEnumerable<Claim> claims, long expiresInSeconds);

public static class BasicJwtTokenExtensions
{
    public static TokenGenerator MakeTokenSigner(this BasicJwtToken tokenParams)
    {
        var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenParams.SecretKey),
            SecurityAlgorithms.HmacSha512Signature);
        return (claims, expiresIn) =>
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(expiresIn),
                SigningCredentials = signingCredentials,
                Issuer = tokenParams.Issuer,
                Audience = tokenParams.Audience
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        };
    }
    
    public static Action<JwtBearerOptions> ConfigureJwtBearer(this BasicJwtToken jwtToken)
    {
        return (opts) =>
        {
            var key = jwtToken.SecretKey;
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtToken.Issuer,
                ValidateIssuer = true,
                ValidAudience = jwtToken.Audience,
                ValidateAudience = true,
            };
        };
    }

}