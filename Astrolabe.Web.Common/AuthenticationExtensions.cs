using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;

namespace Astrolabe.Web.Common;

public static class AuthenticationExtensions
{
    public static AuthenticationBuilder AddAzureAndTokenAuthentication(this IServiceCollection services,
        BasicJwtToken jwtToken, IConfiguration configuration, string azureScheme = "AzureAd")
    {
        services.AddMicrosoftIdentityWebApiAuthentication(configuration, jwtBearerScheme: azureScheme);

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opt =>
            {
                var key = jwtToken.SecretKey;
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtToken.Issuer,
                    ValidateIssuer = true,
                    ValidAudience = jwtToken.Audience,
                    ValidateAudience = true,
                };
            });
    }
}