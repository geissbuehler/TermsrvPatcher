using Microsoft.Win32;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using NetFwTypeLib;
using System.ServiceProcess;

namespace TermsrvPatcher
{
    class Patcher
    {
        public const string Patchfile = "Patches.txt";
        public const int MinByteCount = 4;

        private byte[] termsrvContent;
        public string TermsrvPath { get; }

        public enum Status : int
        {
            Unkown = -1,
            Unpatched = 0,
            Patched = 1
        };

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FreeLibrary(IntPtr hLibModule);

        private IntPtr NSudoDevilModeModuleHandle;

        private void EnableDevilMode()
        {
            if (Environment.Is64BitProcess)
            {
                NSudoDevilModeModuleHandle = LoadLibrary(@"NSudoDevilMode\x64\NSudoDM.dll");
            }
            else
            {
                NSudoDevilModeModuleHandle = LoadLibrary(@"NSudoDevilMode\x86\NSudoDM.dll");
            }
        }

        private void DisableDevilMode()
        {
            FreeLibrary(NSudoDevilModeModuleHandle);
        }

        public bool AllowRdp
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0).Equals(0);
            }
            set
            {
                if (value)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 1, RegistryValueKind.DWord);
                }
            }
        }

        public bool EnableNla
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0).Equals(1);
            }
            set
            {
                if (value)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 1, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", "UserAuthentication", 0, RegistryValueKind.DWord);
                }
            }
        }

        public bool AllowMulti
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 0).Equals(0);
            }
            set
            {
                if (value)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 0, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1, RegistryValueKind.DWord);
                }
            }
        }

        public bool AllowBlank
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 0).Equals(0);
            }
            set
            {
                if (value)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 0, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1, RegistryValueKind.DWord);
                }
            }
        }

        public static List<object> StringsToPatch(string findstrings, string replacestrings)
        {
            string[] findstr = findstrings.Split('|');
            string[] replacestr = replacestrings.Split('|');
            if (findstr.Length != replacestr.Length)
            {
                throw new Exception(String.Format("Hex sub-string count not equal for 'find' and 'replace' hex strings '{0}' and '{1}'", findstrings.Trim(), replacestrings.Trim()));
            }
            List<object> patch = new List<object>();
            for (int count = 0; count < findstr.Length; count++)
            {
                int[] find;
                byte[] replace;
                try
                {
                    find = StrToIntArr(findstr[count]);
                }
                catch
                {
                    throw new Exception(String.Format("Invalid 'find' hex sub-string '{0}'", findstr[count].Trim()));
                }
                try
                {
                    replace = StrToByteArr(replacestr[count]);
                }
                catch
                {
                    throw new Exception(String.Format("Invalid 'replace' hex sub-string '{0}'", replacestr[count].Trim()));
                }
                if (find.Length != replace.Length)
                {
                    throw new Exception(String.Format("Element count in 'find' hex string '{0}' differs from element count in 'replace' hex string '{1}'", findstr[count].Trim(), replacestr[count].Trim()));
                }
                if (find.Length < MinByteCount)
                {
                    throw new Exception(String.Format("Element count in 'find' hex string '{0}' and 'replace' hex string '{1}' less than {2}", findstr[count].Trim(), replacestr[count].Trim(), MinByteCount));
                }
                patch.Add(new List<Object> { find, replace });
            }
            return patch;
        }

        public static List<Object> ReadPatchfile()
        {
            List<Object> patches = new List<Object>();
            List<string> warnings = new List<string>();
            string arch;
            if (Environment.Is64BitProcess)
            {
                arch = "x64";
            }
            else
            {
                arch = "x86";
            }

            StreamReader file = new StreamReader(Patchfile);
            ulong linecount = 0;
            string line;
            while ((line = file.ReadLine()) != null)
            {
                linecount++;
                string trimmed = line.Trim();
                // Skip empty lines and comments
                if ((trimmed.Length == 0) || (trimmed.Substring(0, 1) == "#"))
                {
                    continue;
                }
                string[] patchinfo = trimmed.Split(',');
                if (patchinfo.Length != 3)
                {
                    warnings.Add(String.Format("Element count not equal to 3 at line {0} in patchfile '{1}' ({2})", linecount, Patchfile, trimmed));
                    continue;
                }
                if ((patchinfo[0].Trim() != "x64") && (patchinfo[0].Trim() != "x86"))
                {
                    warnings.Add(String.Format("Architecture '{0}' not equal to 'x86' or 'x64' at line {1} in patchfile '{2}'", patchinfo[0].Trim(), linecount, Patchfile));
                    continue;
                }
                List<object> patch;
                try
                {
                    patch = StringsToPatch(patchinfo[1], patchinfo[2]);
                }
                catch (Exception exception)
                {
                    warnings.Add(String.Format("{0} at line {1} in patchfile '{2}'", exception.Message.ToString(), linecount, Patchfile));
                    continue;
                }
                if (patchinfo[0].Trim() == arch)
                {
                    patches.Add(new List<object>() { patch, linecount });
                }
            }
            file.Close();

            return new List<Object>() { patches, warnings };
        }

            public static string GetTermsrvPath()
        {
            // Be aware that the process must run in 64-bit mode on 64-bit systems (otherwise termserv.dll is only accessible via C:\Windows\Sysnative\termserv.dll)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "termsrv.dll");
        }

        public Patcher()
        {
            TermsrvPath = GetTermsrvPath();
            ReadFile();
        }

        /// <summary>
        /// Reads the content of the specified file into the buffer
        /// </summary>
        /// <param name="path"></param>
        public void ReadFile(string path)
        {
            termsrvContent = File.ReadAllBytes(path);
        }

        /// <summary>
        /// Reada the content of termsrv.dll into the buffer
        /// </summary>
        public void ReadFile()
        {
            ReadFile(TermsrvPath);
        }

        public Status CheckStatus(List<object> patch)
        {
            Status status = Status.Unkown;
            foreach (List<object> subpatch in patch)
            {
                int[] find = (int[])subpatch[0];
                byte[] replace = (byte[])subpatch[1];
                if (FindPattern(replace) == -1)
                {
                    // Replace pattern not found

                    if (FindPattern(find) == -1)
                    {
                        // Find pattern not found

                        status = Status.Unkown;
                        break;
                    }
                    else
                    {
                        // Find pattern found

                        if (status == Status.Patched)
                        {
                            // Opposite result compared to previous loop

                            status = Status.Unkown;
                            break;
                        }
                        else
                        {
                            status = Status.Unpatched;
                        }
                    }
                }
                else
                {
                    // Replace pattern found

                    if (status == Status.Unpatched)
                    {
                        // Opposite result compared to previous loop

                        status = Status.Unkown;
                        break;
                    }
                    else
                    {
                        status = Status.Patched;
                    }
                }
            }
            return status;
        }

        public void StopTermService()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        public void StartTermService()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Stopped)
            {
                sc.Start();
            }
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        public void Patch(List<object> patch)
        {
            string backup = TermsrvPath + "." + GetVersion();
            if (!File.Exists(backup))
            {
                File.Copy(TermsrvPath, backup);
            }
            //FileInfo fi = new FileInfo(TermsrvPath);
            //long l = fi.Length;

            EnableDevilMode();
            foreach (List<object> subpatch in patch)
            {
                int[] find = (int[])subpatch[0];
                byte[] replace = (byte[])subpatch[1];
                int match = FindPattern(find);

                //int matchReplace = FindPattern(replace);


                List<byte> binReplace = new List<byte>();
                foreach (byte hex in replace)
                {
                    binReplace.Add(hex);
                }
                if (binReplace.Count == 0)
                {
                    //DisableDevilMode();
                    // TODO: Throw useful exception here
                }

                using (BinaryWriter writer = new BinaryWriter(File.Open(TermsrvPath, FileMode.Open)))
                {
                    writer.BaseStream.Seek(match, SeekOrigin.Begin);
                    writer.Write(binReplace.ToArray());
                }
            }
            DisableDevilMode();

            // Re-read contents to reflect actual patch status
            ReadFile();
        }

        public void Unpatch()
        {
            if (!BackupAvailable())
            {
                // TODO: Throw useful exception here
            }
            // Read the unpatched file into the buffer
            ReadFile(TermsrvPath + "." + GetVersion());

            EnableDevilMode();
            // To maintain file permisions, write the content of the unpatched file into termsrv.dll (instead of copying the file)
            using (BinaryWriter writer = new BinaryWriter(File.Open(TermsrvPath, FileMode.Open)))
            {
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.Write(termsrvContent);
            }
            DisableDevilMode();

            // Re-read contents to reflect actual patch status
            ReadFile();
        }

        public static int[] StrToIntArr(string pattern)
        {
            List<int> bin = new List<int>();
            foreach (string hex in pattern.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    if (hex == "*")
                    {
                        bin.Add(-1);
                    }
                    else
                    {
                        bin.Add(Convert.ToInt32(hex, 16));
                        // TODO: Catch exception here and raise ArgumentException that returns the string that failed to convert to hex
                    }
                }
            }
            return bin.ToArray();
        }

        public static byte[] StrToByteArr(string pattern)
        {
            List<byte> bin = new List<byte>();
            foreach (string hex in pattern.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    bin.Add(Convert.ToByte(hex, 16));
                }
            }
            return bin.ToArray();
        }

        public static string IntArrToString(int[] data)
        {
            string pattern = "";
            foreach (int integer in data)
            {
                if (pattern != "")
                {
                    pattern += " ";
                }
                if (integer == -1)
                {
                    pattern += "*";
                }
                else
                {
                    pattern += BitConverter.ToString(new byte[] { (byte)integer });
                }
            }
            return pattern;
        }

        public static string ByteArrToString(byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", " ");
        }

        private int FindPattern(int[] searchPattern)
        {
            if (searchPattern.Length == 0)
            {
                throw new ArgumentException("Search pattern is empty");
            }

            int firstHexPos = Array.FindIndex(searchPattern, value => value > -1);
            if (firstHexPos == -1)
            {
                throw new ArgumentException("Search pattern only consists of wildcards");
            }

            int matchPos = -1;
            for (int searchPos = firstHexPos; searchPos < termsrvContent.Length - searchPattern.Length + firstHexPos; searchPos++)
            {
                if (termsrvContent[searchPos] == searchPattern[firstHexPos])
                {
                    matchPos = searchPos - firstHexPos;
                    for (int patternPos = firstHexPos + 1; patternPos < searchPattern.Length - firstHexPos; patternPos++)
                    {
                        if (searchPattern[patternPos] != termsrvContent[searchPos + patternPos - firstHexPos] && searchPattern[patternPos] != -1)
                        {
                            matchPos = -1;
                            break;
                        }
                    }
                }
                if (matchPos > -1)
                {
                    break;
                }
            }

            return matchPos;
        }

        private int FindPattern(byte[] searchPattern)
        {
            if (searchPattern.Length == 0)
            {
                throw new ArgumentException("Search pattern is empty");
            }

            int matchPos = -1;
            for (int searchPos = 0; searchPos < termsrvContent.Length - searchPattern.Length; searchPos++)
            {
                if (termsrvContent[searchPos] == searchPattern[0])
                {
                    matchPos = searchPos;
                    for (int patternPos = 1; patternPos < searchPattern.Length; patternPos++)
                    {
                        if (searchPattern[patternPos] != termsrvContent[searchPos + patternPos])
                        {
                            matchPos = -1;
                            break;
                        }
                    }
                }
                if (matchPos > -1)
                {
                    break;
                }
            }

            return matchPos;
        }

        public string GetVersion()
        {
            return FileVersionInfo.GetVersionInfo(TermsrvPath).ProductVersion;
        }

        public bool BackupAvailable()
        {
            return File.Exists(TermsrvPath + "." + GetVersion());
        }

        public void SetFirewall(bool enabled)
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));
            System.Collections.Generic.IEnumerable<INetFwRule> rules;

            rules = firewallPolicy.Rules.OfType<INetFwRule>().Where(x =>
                x.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN
                // Exclude rules explicitly only for public networks (as seen on some systems)
                && Convert.ToBoolean(x.Profiles & ((int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN))
                && (
                    // Group "Remotedesktop"
                    x.Grouping == "@FirewallAPI.dll,-28752"
                    // Group "Remotedesktop - RemoteFX" (Windows 7)
                    || x.Grouping == "@FirewallAPI.dll,-28852"
                )
            );
            foreach (dynamic rule in rules)
            {
                rule.Enabled = enabled;
            }
        }
    }
}
