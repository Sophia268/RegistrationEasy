using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RegistrationEasy.Services
{
    public static class MachineIdProvider
    {
        public static string GetLocalMachineId()
        {
            try
            {
                string rawId;
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    rawId = GetWindowsId();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    rawId = GetLinuxId();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    rawId = GetMacId();
                }
                else
                {
                    rawId = Environment.MachineName;
                }

                if (string.IsNullOrWhiteSpace(rawId))
                {
                    rawId = Environment.MachineName;
                }

                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
                var hex = BitConverter.ToString(hash, 0, 8).Replace("-", "");
                return FormatMachineId(hex);
            }
            catch
            {
                return FormatMachineId(Environment.MachineName);
            }
        }

        private static string FormatMachineId(string id)
        {
            var sb = new StringBuilder();
            foreach (var c in id)
            {
                if (char.IsLetterOrDigit(c)) sb.Append(char.ToUpperInvariant(c));
            }
            var cleanId = sb.ToString();
            while (cleanId.Length < 16) cleanId += "0";
            if (cleanId.Length > 16) cleanId = cleanId.Substring(0, 16);
            return $"{cleanId.Substring(0, 4)}-{cleanId.Substring(4, 4)}-{cleanId.Substring(8, 4)}-{cleanId.Substring(12, 4)}";
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string GetWindowsId()
        {
            try
            {
                // Use System.Management for backward compatibility on Windows
                var cpu = GetManagementInfo("Win32_Processor", "ProcessorId");
                var board = GetManagementInfo("Win32_BaseBoard", "SerialNumber");
                var disk = GetManagementInfo("Win32_DiskDrive", "SerialNumber");

                if (string.IsNullOrWhiteSpace(cpu) && string.IsNullOrWhiteSpace(board))
                {
                    return Environment.MachineName;
                }
                return $"{cpu}|{board}|{disk}";
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static string GetManagementInfo(string table, string property)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher($"SELECT {property} FROM {table}");
                foreach (var obj in searcher.Get())
                {
                    var val = obj[property]?.ToString();
                    if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
                }
            }
            catch { }
            return string.Empty;
        }

        private static string GetLinuxId()
        {
            try
            {
                if (File.Exists("/etc/machine-id"))
                    return File.ReadAllText("/etc/machine-id").Trim();
                if (File.Exists("/var/lib/dbus/machine-id"))
                    return File.ReadAllText("/var/lib/dbus/machine-id").Trim();
            }
            catch { }
            return Environment.MachineName;
        }

        private static string GetMacId()
        {
            try
            {
                return RunCommand("ioreg", "-rd1 -c IOPlatformExpertDevice");
            }
            catch
            {
                return Environment.MachineName;
            }
        }

        private static string RunCommand(string fileName, string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
