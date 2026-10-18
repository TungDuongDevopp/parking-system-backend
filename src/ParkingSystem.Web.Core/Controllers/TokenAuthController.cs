using Abp.Authorization;
using Abp.Authorization.Users;
using Abp.Runtime.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ParkingSystem.Authentication.JwtBearer;
using ParkingSystem.Authorization;
using ParkingSystem.Authorization.Users;
using ParkingSystem.Models.TokenAuth;
using ParkingSystem.MultiTenancy;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace ParkingSystem.Controllers
{
    [Route("api/[controller]/[action]")]
    [IgnoreAntiforgeryToken]
    public class TokenAuthController : ParkingSystemControllerBase
    {
        private readonly LogInManager _logInManager;
        private readonly AbpLoginResultTypeHelper _abpLoginResultTypeHelper;
        private readonly TokenAuthConfiguration _configuration;
        private readonly Microsoft.AspNetCore.Antiforgery.IAntiforgery _antiforgery;

        public TokenAuthController(
            LogInManager logInManager,
            AbpLoginResultTypeHelper abpLoginResultTypeHelper,
            TokenAuthConfiguration configuration,
            Microsoft.AspNetCore.Antiforgery.IAntiforgery antiforgery)
        {
            _logInManager = logInManager;
            _abpLoginResultTypeHelper = abpLoginResultTypeHelper;
            _configuration = configuration;
            _antiforgery = antiforgery;
        }

        [HttpPost]
        public async Task<AuthenticateResultModel> Authenticate([FromBody] AuthenticateModel model)
        {
            var loginResult = await GetLoginResultAsync(
                model.UserNameOrEmailAddress,
                model.Password,
                null
            );

            var accessToken = CreateAccessToken(CreateJwtClaims(loginResult.Identity));

            return new AuthenticateResultModel
            {
                AccessToken = accessToken,
                EncryptedAccessToken = GetEncryptedAccessToken(accessToken),
                ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                UserId = loginResult.User.Id
            };
        }

        [HttpPost]
        public async Task<IActionResult> BrowserAuthenticate([FromBody] AuthenticateModel model)
        {
            var loginResult = await GetLoginResultAsync(
                model.UserNameOrEmailAddress,
                model.Password,
                null
            );

            var accessToken = CreateAccessToken(
                CreateJwtClaims(loginResult.Identity)
            );

            // 1. Append JWT access_token cookie
            Response.Cookies.Append(
                "access_token",
                accessToken,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.Add(
                        _configuration.Expiration
                    )
                }
            );

            // 2. Establish identity in HttpContext using the validated JWT ClaimsPrincipal
            // to ensure Issuer, ClaimTypes (mapped), and Claims strictly match subsequent JWT requests
            HttpContext.User = CreatePrincipalFromToken(accessToken);

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            // 3. Set XSRF-TOKEN cookie for same-origin / Swagger / proxy clients (readable by JS)
            Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.Add(_configuration.Expiration)
                }
            );

            // 4. Expose X-XSRF-TOKEN header and return XsrfToken in JSON response for cross-origin clients (e.g. React SPA)
            Response.Headers["X-XSRF-TOKEN"] = tokens.RequestToken;

            return Ok(new
            {
                UserId = loginResult.User.Id,
                ExpireInSeconds = (int)_configuration.Expiration.TotalSeconds,
                XsrfToken = tokens.RequestToken
            });
        }

        [HttpGet]
        public IActionResult GetXsrfToken()
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            var tokens = _antiforgery.GetAndStoreTokens(HttpContext);

            Response.Cookies.Append(
                "XSRF-TOKEN",
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Lax,
                    Path = "/"
                }
            );

            Response.Headers["X-XSRF-TOKEN"] = tokens.RequestToken;

            return Ok(new { xsrfToken = tokens.RequestToken });
        }

        private ClaimsPrincipal CreatePrincipalFromToken(string accessToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _configuration.SecurityKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration.Issuer,
                ValidateAudience = true,
                ValidAudience = _configuration.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            return tokenHandler.ValidateToken(accessToken, validationParameters, out _);
        }

        private async Task<AbpLoginResult<Tenant, User>> GetLoginResultAsync(string usernameOrEmailAddress, string password, string tenancyName)
        {
            var loginResult = await _logInManager.LoginAsync(usernameOrEmailAddress, password, tenancyName);

            switch (loginResult.Result)
            {
                case AbpLoginResultType.Success:
                    return loginResult;
                default:
                    throw _abpLoginResultTypeHelper.CreateExceptionForFailedLoginAttempt(loginResult.Result, usernameOrEmailAddress, tenancyName);
            }
        }

        private string CreateAccessToken(IEnumerable<Claim> claims, TimeSpan? expiration = null)
        {
            var now = DateTime.UtcNow;

            var jwtSecurityToken = new JwtSecurityToken(
                issuer: _configuration.Issuer,
                audience: _configuration.Audience,
                claims: claims,
                notBefore: now,
                expires: now.Add(expiration ?? _configuration.Expiration),
                signingCredentials: _configuration.SigningCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);
        }

        private static List<Claim> CreateJwtClaims(ClaimsIdentity identity)
        {
            var claims = identity.Claims.ToList();
            var nameIdClaim = claims.First(c => c.Type == ClaimTypes.NameIdentifier);

            // Specifically add the jti (random nonce), iat (issued timestamp), and sub (subject/user) claims.
            claims.AddRange(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, nameIdClaim.Value),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.Now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            });

            return claims;
        }

        private string GetEncryptedAccessToken(string accessToken)
        {
            return SimpleStringCipher.Instance.Encrypt(accessToken);
        }
    }
}
