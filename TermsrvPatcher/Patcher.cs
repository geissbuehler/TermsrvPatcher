using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;
using PrivilegeClass;
using System.Collections.Generic;
using Microsoft.Win32;

namespace TermsrvPatcher
{
    class Patcher
    {
        private byte[] termsrvContent;
        public string termsrvPath { get; }
        public bool allowRdp
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 1).Equals(0);
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
        public bool allowMulti
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1).Equals(0);
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
        public bool allowBlank
        {
            get
            {
                return Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1).Equals(0);
            }
            set
            {
                if (value)
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1, RegistryValueKind.DWord);
                }
                else
                {
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 0, RegistryValueKind.DWord);
                }
            }
        }

        public static string getTermsrvPath()
        {
            // Be aware that the process must run in 64-bit mode on 64-bit systems (otherwise termserv.dll is only accessible via C:\Windows\Sysnative\termserv.dll)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "termsrv.dll");
        }

        public Patcher()
        {
            termsrvPath = getTermsrvPath();
            readFile();
        }

        public Patcher(string path)
        {
            termsrvPath = path;
            readFile();
        }

        /// <summary>
        /// Reads the content of the specified file into the buffer
        /// </summary>
        /// <param name="path"></param>
        public void readFile(string path)
        {
            termsrvContent = File.ReadAllBytes(path);
        }

        /// <summary>
        /// Reada the content of termsrv.dll into the buffer
        /// </summary>
        public void readFile()
        {
            readFile(termsrvPath);
        }

        public int checkStatus(string find, string replace)
        {
            int[] findArr = strToIntArr(find);
            byte[] replaceArr = strToByteArr(replace);

            if (findPattern(replaceArr) == -1)
            {
                if(findPattern(findArr) == -1)
                {
                    // Patch status unknown
                    return -1;
                }
                else
                {
                    // Unpatched
                    return 0;
                }
            }
            else
            {
                // Patched
                return 1;
            }
        }

        public void patch(string find, string replace)
        {
            int[] findArr = strToIntArr(find);
            int match = findPattern(findArr);

            byte[] replaceArr = strToByteArr(replace);
            int matchReplace = findPattern(replaceArr);

            string backup = termsrvPath + "." + getVersion();
            if (!File.Exists(backup))
            {
                //textBlockMessages.Text += "Backup file...";
                File.Copy(termsrvPath, backup);
                //textBlockMessages.Text += " OK";
            }
            //takeOwnership(getAdministratorsIdentity());
            //setFullControl(getAdministratorsIdentity());
            FileInfo fi = new FileInfo(termsrvPath);
            long l = fi.Length;

            List<byte> binReplace = new List<byte>();
            foreach (string hex in replace.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    binReplace.Add(System.Convert.ToByte(hex, 16));
                }
            }
            if (binReplace.Count == 0)
            {
                // Exception
            }

            using (BinaryWriter writer = new BinaryWriter(File.Open(termsrvPath, FileMode.Open)))
            {
                writer.BaseStream.Seek(match, SeekOrigin.Begin);
                writer.Write(binReplace.ToArray());
            }

            // Re-read contents to reflect actual patch status
            readFile();
        }

        public void unpatch()
        {
            string backup = termsrvPath + "." + getVersion();
            if (!File.Exists(backup))
            {
                // Exception
            }
            // Read the unpatched file into the buffer
            readFile(backup);

            // Write the content of the unpatched file into termsrv.dll (insted of copying the file to maintain file permisions)
            using (BinaryWriter writer = new BinaryWriter(File.Open(termsrvPath, FileMode.Open)))
            {
                writer.BaseStream.Seek(0, SeekOrigin.Begin);
                writer.Write(termsrvContent);
            }

            // Re-read contents to reflect actual patch status
            readFile();
        }

        public void unpatch_old()
        {

            string backup = termsrvPath + "." + getVersion();
            if (File.Exists(backup))
            {
                File.Copy(backup, termsrvPath);
            }

            // Re-read contents to reflect actual patch status
            readFile();
        }

        private int[] strToIntArr(string pattern)
        {
            int firstHex = -1;
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
                        if (firstHex == -1)
                        {
                            // Position of first non-astersik
                            firstHex = bin.Count;
                        }
                        bin.Add(Convert.ToInt32(hex, 16));
                    }
                }
            }
            if (firstHex == -1)
            {
                return new int[] { };
            }
            return bin.ToArray();
        }

        private byte[] strToByteArr(string pattern)
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

        private int findPattern(int[] searchPattern)
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

        private int findPattern(byte[] searchPattern)
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

        public string getVersion()
        {
            return FileVersionInfo.GetVersionInfo(termsrvPath).ProductVersion;
        }

        public IdentityReference getAdministratorsIdentity()
        {
            return new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        }

        public void takeOwnership(IdentityReference identity)
        {
            FileInfo fi = new FileInfo(termsrvPath);
            FileSecurity fs = fi.GetAccessControl();
            Privilege p = new Privilege(Privilege.TakeOwnership);
            p.Enable();
            fs.SetOwner(identity);
            File.SetAccessControl(termsrvPath, fs); //Update the Access Control on the File
            p.Revert();
        }

        public void setFullControl(IdentityReference identity)
        {
            FileInfo fi = new FileInfo(termsrvPath);
            FileSecurity fs = fi.GetAccessControl();

            fs.SetAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(termsrvPath, fs);
        }
    }
}
