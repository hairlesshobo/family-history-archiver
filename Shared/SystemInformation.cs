using System;
using System.Runtime.InteropServices;
using Archiver.Shared.Models;
using Archiver.Shared.Utilities;

namespace Archiver.Shared
{
    public static class SystemInformation
    {
        private static OSType _osType = OSType.Unknown;


        public static OSType OperatingSystemType => _osType;
        public static Architecture Architecture => System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
        public static string Description => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        public static string Identifier => System.Runtime.InteropServices.RuntimeInformation.RuntimeIdentifier;
        
        
        static SystemInformation()
        {            
            if (OperatingSystem.IsWindows())
                _osType = OSType.Windows;
            else if (OperatingSystem.IsLinux())
                _osType = OSType.Linux;

            // these are commented out because we have not yet built support for them into the app. For now, they will 
            // be identified as "Unknown" and the app will not lauch on an unknown platform.
            // else if (OperatingSystem.IsFreeBSD())
            //     _osType = OSType.FreeBSD;
            // else if (OperatingSystem.IsMacOS())
            //     _osType = OSType.OSX;
        }

        public static void WriteSystemInfo(bool color = false)
        {
            if (color)
            {
                Formatting.WriteLineC(ConsoleColor.Green, "System Information:");
                Formatting.WriteC(ConsoleColor.Cyan, "  OS Platform: ");
                Console.WriteLine($"{SystemInformation.OperatingSystemType.ToString()} ({SystemInformation.Architecture.ToString()})");
                Formatting.WriteC(ConsoleColor.Cyan, "  Description: ");
                Console.WriteLine(SystemInformation.Description);
                Formatting.WriteC(ConsoleColor.Cyan, "  Identifier: ");
                Console.WriteLine(SystemInformation.Identifier);
            }
            else
            {
                Console.WriteLine("System Information:");
                Console.WriteLine($"  OS Platform: {SystemInformation.OperatingSystemType.ToString()} ({SystemInformation.Architecture.ToString()})");
                Console.WriteLine($"  Description: {SystemInformation.Description}");
                Console.WriteLine($"  Identifier: {SystemInformation.Identifier}");
            }
        }
    }
}