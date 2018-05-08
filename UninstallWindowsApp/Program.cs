using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UninstallWindowsApp
{
    class Program
    {
        static void Main(string[] args)
        {
            bool isAdministrator = IsUserAdministrator();
            Console.WriteLine("Current user is administrator - {0}", isAdministrator);
            if (isAdministrator)
            {
                Uninstall(args[0]);
            }
        }

        public static void Uninstall(string appName)
        {
           string product_guid = GetRegistryKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall", appName);
            if (!product_guid.Equals(""))
            {
                string uninstallString = String.Format("/x{0} /qb", product_guid);
                Console.WriteLine("Uninstall string {0}", uninstallString);
                Start(uninstallString);
            }
        }

        public static Process Start(string arguments)
        {
            ProcessStartInfo info = new ProcessStartInfo("msiexec.exe", arguments);
            Console.WriteLine("Starting process");
            Process process = Process.Start(info);
            Console.WriteLine("Waiting for 10 seconds");
            Thread.Sleep(Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds));

            while (!process.HasExited)
            {
                Thread.Sleep(Convert.ToInt32(TimeSpan.FromSeconds(10).TotalMilliseconds));
            }
            return process;
        }

        private static bool IsUserAdministrator()
        {
            bool isAdmin;
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch (UnauthorizedAccessException ex)
            {
                isAdmin = false;
            }
            catch (Exception ex)
            {
                isAdmin = false;
            }
            return isAdmin;
        }


        public static string GetRegistryKey(string registryKey, string registryName)
        {
            string product_guid = String.Empty;
            string version = String.Empty;

            RegistryView type = RegistryView.Registry32;
            string subskey = GetSubKey(type, registryKey, registryName);
            if (subskey.Equals(String.Empty))
            {
                type = RegistryView.Registry64;
                subskey = GetSubKey(type, registryKey, registryName);
            }

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, type))
            {
                using (RegistryKey key = hklm.OpenSubKey(registryKey))
                {

                    using (RegistryKey subkey = key.OpenSubKey(subskey))
                    {
                        product_guid = subskey;
                        version = (string)subkey.GetValue("DisplayVersion");
                    }
                }
            }

            return product_guid;
        }

        private static string GetSubKey(RegistryView type, string registryKey, string registryName)
        {
            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, type))
            {
                using (RegistryKey key = hklm.OpenSubKey(registryKey))
                {
                    foreach (string subkey_name in key.GetSubKeyNames())
                    {
                        using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                        {
                            string sub = subkey.ToString();
                            string displayName = (string)subkey.GetValue("DisplayName");
                            var g = key.GetSubKeyNames();
                            if (!(displayName == null))
                            {

                                if (displayName.Contains(registryName))
                                {
                                    return subkey_name;
                                }
                            }
                        }
                    }
                }
            }
            return String.Empty;
        }

    }
}
