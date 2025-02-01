using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using PresentationLayer.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace PresentationLayer.ExtensionMethods
{
    public static class HtmlHelperExtensionMethods
    {
        public static string Translate(this IHtmlHelper helper, string key)
        {
            IServiceProvider service = helper.ViewContext.HttpContext.RequestServices;
            IStringLocalizer localizer = service.GetRequiredService<IStringLocalizer>();
            string result = localizer[key];
            return result;
        }
    }
}
