using System.Reflection;
using Microsoft.Extensions.Localization;

namespace PresentationLayer.Utilities
{
    public class SharedViewLocalizer
    {
        private readonly IStringLocalizerFactory _factory;

        public SharedViewLocalizer(IStringLocalizerFactory factory)
        {
            _factory = factory;
        }

        public IStringLocalizer GetLocalizer(string resourceName)
        {
            var assemblyName = typeof(SharedResource).GetTypeInfo().Assembly.GetName().Name;
            return _factory.Create(resourceName, assemblyName);
        }

        public Dictionary<string, string> GetAllLocalizedStrings(string resourceName)
        {
            var localizer = GetLocalizer(resourceName);
            return localizer.GetAllStrings().ToDictionary(ls => ls.Name, ls => ls.Value);
        }

    }


}
