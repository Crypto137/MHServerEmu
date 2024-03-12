using System.Globalization;
using System.Reflection;

namespace MHServerEmu.Core.Helpers
{
    public static class AssemblyHelper
    {
        /// <summary>
        /// Returns the version of the calling assembly as a <see cref="string"/>.
        /// </summary>
        public static string GetAssemblyVersion()
        {
            var assembly = Assembly.GetCallingAssembly();
            return assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Returns the informational version of the calling assembly as a <see cref="string"/>.
        /// </summary>
        public static string GetAssemblyInformationalVersion()
        {
            var assembly = Assembly.GetCallingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            // Make sure the informational version attribute is defined
            if (attribute?.InformationalVersion == null)
                return string.Empty;

            // Return everything that comes before build time
            return attribute.InformationalVersion.Split('+')[0];
        }

        /// <summary>
        /// Parses build time of the calling assembly.
        /// </summary>
        public static DateTime ParseAssemblyBuildTime(string prefix = "+build", string format = "yyyyMMddHHmmss")
        {
            var assembly = Assembly.GetCallingAssembly();
            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            // Make sure the attribute where we store our build time is defined
            if (attribute?.InformationalVersion == null)
                return default;

            // Find the build time prefix
            int buildTimeIndex = attribute.InformationalVersion.IndexOf(prefix);
            if (buildTimeIndex == -1)
                return default;

            // Parse our build time from the attribute
            string buildTimeString = attribute.InformationalVersion[(buildTimeIndex + prefix.Length)..];
            if (DateTime.TryParseExact(buildTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var buildTime) == false)
                return default;

            return buildTime;
        }
    }
}
