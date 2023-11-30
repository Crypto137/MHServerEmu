using System.Diagnostics;
using System.Text;

namespace MHSourceFilePathExtractor
{
    public class ExecutableLoader
    {
        private static readonly byte[] ShippingSignature = Convert.FromHexString("5368697070696E675C"); // Shipping\

        public bool IsValid { get; }
        public bool IsShipping { get; }
        public string Version { get; }

        public byte[] Data { get; }

        public ExecutableLoader(string filePath)
        {
            Console.WriteLine($"Analyzing {filePath}...");

            // Check if the file even exists
            if (File.Exists(filePath) == false)
            {
                Console.WriteLine($"{filePath} is not a valid file path");
                IsValid = false;
                return;
            }

            // Check version info
            var versionInfo = FileVersionInfo.GetVersionInfo(filePath);

            if (versionInfo.CompanyName != "Gazillion, Inc.")
            {
                Console.WriteLine($"{filePath} is not a Marvel Heroes executable");
                IsValid = false;
                return;
            }

            Version = versionInfo.FileVersion.Replace(',', '.');

            // Load the executable and check for shipping signature
            Data = File.ReadAllBytes(filePath);
            IsShipping = CheckShippingSignature();

            Console.WriteLine($"Detected version: {BuildVersionString()}");
            IsValid = true;
        }

        private bool CheckShippingSignature()
        {
            // Hack: speed this up by starting near the end of the executable where the build config
            // signatures we are looking for should be.
            for (int i = Data.Length - Data.Length / 5; i < Data.Length; i++)
            {
                if (ShippingSignature.SequenceEqual(Data.Skip(i).Take(ShippingSignature.Length)))
                    return true;
            }

            return false;
        }

        private string BuildVersionString()
        {
            StringBuilder sb = new();
            sb.Append(Version);
            sb.Append(" (").Append(IsShipping ? "Shipping" : "Internal").Append(')');
            return sb.ToString();
        }
    }
}
