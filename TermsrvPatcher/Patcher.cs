using System;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Security.AccessControl;
using PrivilegeClass;

namespace TermsrvPatcher
{
    static class Patcher

    {
        // Be aware that the process must run in 64-bit mode on 64-bit systems (otherwise termserv.dll is only accessible as C:\Windows\Sysnative\termserv.dll)
        public static string termsrvPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "termsrv.dll");

        public static string getVersion(string filePath)
        {
            return FileVersionInfo.GetVersionInfo(filePath).ProductVersion;
        }

        public static IdentityReference getAdministratorsIdentity()
        {
            return new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        }

        public static void takeOwnership(string filePath, IdentityReference identity)
        {
            FileInfo fi = new FileInfo(filePath);
            FileSecurity fs = fi.GetAccessControl();
            Privilege p = new Privilege(Privilege.TakeOwnership);
            p.Enable();
            fs.SetOwner(identity);
            File.SetAccessControl(filePath, fs); //Update the Access Control on the File
            p.Revert();
        }

        public static void setFullControl(string filePath, IdentityReference identity)
        {
            FileInfo fi = new FileInfo(filePath);
            FileSecurity fs = fi.GetAccessControl();

            fs.SetAccessRule(new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(filePath, fs);
        }
    }
}
