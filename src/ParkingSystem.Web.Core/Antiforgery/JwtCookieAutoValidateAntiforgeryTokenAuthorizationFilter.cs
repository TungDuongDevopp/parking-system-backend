using System;
using System.Threading.Tasks;
using Abp.Dependency;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ParkingSystem.Web.Antiforgery
{
    public class JwtCookieAutoValidateAntiforgeryTokenAuthorizationFilter : IAsyncAuthorizationFilter, IAntiforgeryPolicy, ITransientDependency
    {
        public const string AuthSourceItemKey = "ValidatedAuthSource";
        public const string AuthSourceBearer = "BearerHeader";
        public const string AuthSourceCookie = "Cookie";

        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<JwtCookieAutoValidateAntiforgeryTokenAuthorizationFilter> _logger;

        public JwtCookieAutoValidateAntiforgeryTokenAuthorizationFilter(
            IAntiforgery antiforgery,
            ILogger<JwtCookieAutoValidateAntiforgeryTokenAuthorizationFilter> logger = null)
        {
            _antiforgery = antiforgery ?? throw new ArgumentNullException(nameof(antiforgery));
            _logger = logger ?? NullLogger<JwtCookieAutoValidateAntiforgeryTokenAuthorizationFilter>.Instance;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // 1. Honor [IgnoreAntiforgeryToken] and other effective IAntiforgeryPolicy implementations
            if (!context.IsEffectivePolicy<IAntiforgeryPolicy>(this))
            {
                _logger.LogDebug("Skipping Antiforgery validation because a more effective IAntiforgeryPolicy (e.g. [IgnoreAntiforgeryToken]) is applied.");
                return;
            }

            // 2. Validate request when required
            if (ShouldValidate(context))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context.HttpContext);
                }
                catch (AntiforgeryValidationException ex)
                {
                    _logger.LogWarning("Antiforgery token validation failed: {Message}", ex.Message);
                    context.Result = new AntiforgeryValidationFailedResult();
                }
            }
        }

        protected virtual bool ShouldValidate(AuthorizationFilterContext context)
        {
            var httpContext = context.HttpContext;
            var method = httpContext.Request.Method;

            // Rule 4: Safe methods (GET, HEAD, OPTIONS, TRACE) do not modify state and never require CSRF
            if (HttpMethods.IsGet(method) ||
                HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) ||
                HttpMethods.IsTrace(method))
            {
                return false;
            }

            // Rule 7: Unauthenticated requests should be handled by Authentication / AbpAuthorizationFilter (401 Unauthorized),
            // not rejected with 400 Bad Request by Antiforgery filter.
            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            // Rule 1 & 3: If actually authenticated via Bearer Header, bypass CSRF
            var validatedAuthSource = httpContext.Items[AuthSourceItemKey] as string;
            if (string.Equals(validatedAuthSource, AuthSourceBearer, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Rule 2 & 3: If authenticated via Cookie, MUST validate CSRF token
            if (string.Equals(validatedAuthSource, AuthSourceCookie, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Fallback: If cookie "access_token" was present on this authenticated mutating request
            if (httpContext.Request.Cookies.ContainsKey("access_token"))
            {
                return true;
            }

            return false;
        }
    }
}
