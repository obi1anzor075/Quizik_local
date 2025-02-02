using Microsoft.AspNetCore.Localization;

namespace PresentationLayer
{
    public class CultureHelper
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public CultureHelper(IHttpContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public string GetCurrentCulture()
        {
            var requestCulture = _contextAccessor.HttpContext?.Features.Get<IRequestCultureFeature>();
            return requestCulture?.RequestCulture.UICulture.Name ?? "ru-RU";
        }
    }
}
