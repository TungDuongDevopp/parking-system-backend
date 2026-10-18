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

            // Rule 1: Safe methods (GET, HEAD, OPTIONS, TRACE) do not modify state and never require CSRF.
            if (HttpMethods.IsGet(method) ||
                HttpMethods.IsHead(method) ||
                HttpMethods.IsOptions(method) ||
                HttpMethods.IsTrace(method))
            {
                return false;
            }

            // Rule 5: If no access_token cookie is present, the request is not using cookie-based auth.
            // This covers: fully anonymous requests, pure Bearer-only requests, and idempotent logout
            // calls after the session cookie has already been deleted. Skip CSRF in all these cases.
            if (!httpContext.Request.Cookies.ContainsKey("access_token"))
            {
                return false;
            }

            // From here on, the access_token cookie IS present in this request.
            // We must determine the auth source that was actually accepted by the JWT handler.
            var validatedAuthSource = httpContext.Items[AuthSourceItemKey] as string;

            // Rule 2: Bearer header takes priority over cookie (see JwtTokenResolver).
            // If the JWT handler confirmed it used the Bearer header, the cookie was not the
            // credential — skip CSRF.
            if (string.Equals(validatedAuthSource, AuthSourceBearer, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Rule 3 & 4: Cookie is present AND the auth source is either:
            //   - Confirmed "Cookie" (OnTokenValidated fired with AuthSource = Cookie), OR
            //   - Unknown / null (authentication may have failed, e.g. expired token, but the browser
            //     still presented the cookie — we cannot distinguish this from a valid cookie request
            //     without CSRF, so we enforce validation to be safe).
            // In both cases CSRF MUST be validated via IAntiforgery.ValidateRequestAsync().
            return true;
        }
    }
}
