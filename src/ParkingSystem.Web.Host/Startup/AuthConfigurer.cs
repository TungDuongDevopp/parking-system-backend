using Abp.Runtime.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParkingSystem.Web.Host.Startup
{
    public static class AuthConfigurer
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            if (bool.Parse(configuration["Authentication:JwtBearer:IsEnabled"]))
            {
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "JwtBearer";
                    options.DefaultChallengeScheme = "JwtBearer";
                }).AddJwtBearer("JwtBearer", options =>
                {
                    options.Audience = configuration["Authentication:JwtBearer:Audience"];

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // The signing key must match!
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["Authentication:JwtBearer:SecurityKey"])),

                        // Validate the JWT Issuer (iss) claim
                        ValidateIssuer = true,
                        ValidIssuer = configuration["Authentication:JwtBearer:Issuer"],

                        // Validate the JWT Audience (aud) claim
                        ValidateAudience = true,
                        ValidAudience = configuration["Authentication:JwtBearer:Audience"],

                        // Validate the token expiry
                        ValidateLifetime = true,

                        // If you want to allow a certain amount of clock drift, set that here
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = JwtTokenResolver
                    };
                });
            }
        }
       
        private static Task JwtTokenResolver(MessageReceivedContext context)
        {
            var request = context.HttpContext.Request;
            request.Headers["Cookie"].ToString();

            // 1. Preserve Authorization header priority.
            if (request.Headers.ContainsKey("Authorization"))
            {
                return Task.CompletedTask;
            }

            // 2. Preserve SignalR encrypted query token.
            if (request.Path.HasValue &&
                request.Path.Value.StartsWith(
                    "/signalr",
                    StringComparison.OrdinalIgnoreCase))
            {
                var encryptedToken =
                    request.Query["enc_auth_token"].FirstOrDefault();

                if (!string.IsNullOrEmpty(encryptedToken))
                {
                    context.Token =
                        SimpleStringCipher.Instance.Decrypt(encryptedToken);

                    return Task.CompletedTask;
                }
            }

            // 3. Read JWT from cookie.
            if (request.Cookies.TryGetValue("access_token", out var cookieToken) &&
                !string.IsNullOrWhiteSpace(cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        }     
    }
}
