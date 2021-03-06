﻿using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Forms;

using Microsoft.Win32;

namespace RobloxStudioModManager
{
    static class Program
    {
        public static RegistryKey ModManagerRegistry = GetSubKey(Registry.CurrentUser, "SOFTWARE", "Roblox Studio Mod Manager");
        private static string _ = ""; // Default key/value used for stuff in UpdateStudioRegistryProtocols.

        public static RegistryKey GetSubKey(RegistryKey key, params string[] path)
        {
            string constructedPath = "";
            foreach (string p in path)
                constructedPath = Path.Combine(constructedPath, p);

            return key.CreateSubKey(constructedPath, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.None);
        }

        // This sets up the following:
        // 1: The File Protocol to open .rbxl/.rbxlx files using my mod manager.
        // 2: The URI Protcol to open places from the website through my mod manager.

        public static void UpdateStudioRegistryProtocols(string setupDir, string buildName, string robloxStudioBetaPath)
        {
            string modManagerPath = Application.ExecutablePath;

            // Register the base "Roblox.Place" open protocol.
            RegistryKey classes = GetSubKey(Registry.CurrentUser, "SOFTWARE", "Classes");
            RegistryKey robloxPlace = GetSubKey(classes, "Roblox.Place");
            robloxPlace.SetValue(_, "Roblox Place");

            RegistryKey robloxPlaceCmd = GetSubKey(robloxPlace, "shell", "open", "command");
            robloxPlaceCmd.SetValue(_, '"' + modManagerPath + "\" -task EditFile -localPlaceFile \"%1\"");

            // Pass the .rbxl and .rbxlx file formats to Roblox.Place
            RegistryKey[] robloxLevelPass = { GetSubKey(classes, ".rbxl"), GetSubKey(classes, ".rbxlx") };
            foreach (RegistryKey rbxLevel in robloxLevelPass)
            {
                rbxLevel.SetValue(_, "Roblox.Place");
                GetSubKey(rbxLevel, "Roblox.Place", "ShellNew");
                rbxLevel.Close();
            }

            // Setup the URI protocol for opening the mod manager through the website.
            RegistryKey robloxStudioUrl = GetSubKey(classes, "roblox-studio");
            robloxStudioUrl.SetValue(_, "URL: Roblox Protocol");
            robloxStudioUrl.SetValue("URL Protocol", _);

            RegistryKey studioUrlCmd = GetSubKey(robloxStudioUrl, "shell", "open", "command");
            studioUrlCmd.SetValue(_, modManagerPath + " %1");

            // Set the default icon for both protocols.
            RegistryKey[] appReg = { robloxPlace, robloxStudioUrl };
            foreach (RegistryKey app in appReg)
            {
                RegistryKey defaultIcon = GetSubKey(app, "DefaultIcon");
                defaultIcon.SetValue(_, modManagerPath + ",0");
            }
        }
        private static bool validateCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            if (errors == SslPolicyErrors.None)
                return true;
            else if (errors == SslPolicyErrors.RemoteCertificateNameMismatch)
                return certificate.Subject.Contains("Amazon.com Inc.");
            else 
                return false;
        }

        [STAThread]
        static void Main(string[] args)
        {
            // Delete deprecated startup protocol if it exists.
            bool registryInit = bool.Parse(ModManagerRegistry.GetValue("Initialized Startup Protocol", "False") as string);
            if (registryInit)
            {
                string myPath = Application.ExecutablePath;
                RegistryKey startUpBin = GetSubKey(Registry.CurrentUser, "SOFTWARE", "Microsoft", "Windows", "CurrentVersion", "Run");
                startUpBin.DeleteValue("RobloxStudioModManager");
                ModManagerRegistry.SetValue("Initialized Startup Protocol", false);
            }

            // Add Roblox HTTPS validation 
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(validateCertificate);

            // Start launcher
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Launcher(args));
        }
    }
}
