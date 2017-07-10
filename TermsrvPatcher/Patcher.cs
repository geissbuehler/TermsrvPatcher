using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;
using PrivilegeClass;
using System.Collections.Generic;
using System.Linq;

namespace TermsrvPatcher
{
    class Patcher
    {
        public string termsrvPath { get; }
        private byte[] termsrvContent;

        public static string getTermsrvPath()
        {
            // Be aware that the process must run in 64-bit mode on 64-bit systems (otherwise termserv.dll is only accessible via C:\Windows\Sysnative\termserv.dll)
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "termsrv.dll");
        }

        public Patcher()
        {
            termsrvPath = getTermsrvPath();
            termsrvContent = File.ReadAllBytes(termsrvPath);
        }

        public Patcher(string path)
        {
            termsrvPath = path;
            termsrvContent = File.ReadAllBytes(termsrvPath);
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
            takeOwnership(getAdministratorsIdentity());
            setFullControl(getAdministratorsIdentity());
            FileInfo fi = new FileInfo(termsrvPath);
            long l = fi.Length;
            //string find = "39 81 3C 06 00 00 0F 84 53 71 02 00";
            //string replace = "B8 00 01 00 00 89 81 38 06 00 00 90";
            /*int? firstHex = null; // handles asterisks at beginning
            List<int> findArr = new List<int>();
            foreach (string hex in find.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    if (hex == "*")
                    {
                        findArr.Add(-1);
                    }
                    else
                    {
                        if (firstHex == null)
                        {
                            // Position of first non-astersik
                            firstHex = findArr.Count;
                        }
                        findArr.Add(System.Convert.ToInt32(hex, 16));
                    }
                }
            }
            if (findArr.Count == 0)
            {
                // Exception
            }
            if (firstHex == null)
            {
                // Exception here
            }*/

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

            /*int match = findPattern(findArr.ToArray());

            if (match == -1)
            {
                // Exception
            }*/

            using (BinaryWriter writer = new BinaryWriter(File.Open(termsrvPath, FileMode.Open)))
            {
                writer.BaseStream.Seek(match, SeekOrigin.Begin);
                writer.Write(binReplace.ToArray());
            }
        }

        public int[] strToIntArr(string pattern)
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

        public byte[] strToByteArr(string pattern)
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

        private int _findPattern(int[] pattern)
        {
            //var firstHex = pattern.Where(value => value >-1);
            //pattern.SequenceEqual;
            //pattern.Take;
            //pattern.;
            int firstHexPos = Array.FindIndex(pattern, value => value > -1);

            if (firstHexPos == -1)
            {
                throw new Exception();
            }

            int matchPos;
            int searchStartPos = firstHexPos;
            int idx;
            do
            {
                idx = Array.FindIndex(termsrvContent.Skip(searchStartPos).ToArray(), value => value == pattern[firstHexPos]);
                if (idx == -1)
                {
                    // Exception?
                    matchPos = -1;
                    break;
                }
                searchStartPos += idx;
                if (searchStartPos + pattern.Length - firstHexPos > termsrvContent.Length) // -1?
                {
                    // Exception?
                    matchPos = -1;
                    break;
                }
                matchPos = searchStartPos - firstHexPos;
                for (int searchPos = firstHexPos; searchPos < pattern.Length; searchPos++)
                {
                    if (pattern[searchPos] != -1 && pattern[searchPos] != termsrvContent[searchStartPos + searchPos])
                    {
                        searchStartPos++;
                        matchPos = -1;
                        break;
                    }
                }
            } while (matchPos == -1);

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
