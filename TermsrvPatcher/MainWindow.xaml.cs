using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System;
using Microsoft.Win32;
using System.ServiceProcess;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            radioButtonEnableRdp.IsChecked = true;
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Prepares a copy of termsrv.dll for testing the processing of taking ownership, getting full control and patching the file.
            /// Uses robocopy to prepare a copy of C:\Windows\system32\termsrv.dll in the folder C:\termsrv with exact the same persmissions and ownership for the file and the containing folder.
            /// </summary>

            //RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true);
            // Enable RDP
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryValueKind.DWord);

            // Disable multiple sessions per user
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1, RegistryValueKind.DWord);

            // Disable remote logon for user accounts that are not password protected
            //Registry.SetValue(@"HKLM\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1, RegistryValueKind.DWord);

            string testFolder = @"C:\termsrv";
            string termsrvPath = Patcher.getTermsrvPath();

            //// Ensure robocopy is able to to re-prepare the folder if the directory already exists with restricted permissions from a previous test
            // Take ownership of C:\termsrv and everything inside
            Process.Start("takeown", String.Format(@"/a /r /f {0}\", testFolder)).WaitForExit();
            // Grant full control for the Administrators group
            Process.Start("ICACLS", String.Format(@"{0}\ /Grant *S-1-5-32-544:F /t", testFolder)).WaitForExit();

            // Replicate folder C:\Windows\system32\termsrv.dll as C:\termsrv with all permissions, synch only termserv.* files (includes deleting in the target)
            Process.Start("robocopy.exe", String.Format(@"/mir /lev:1 /copyall /secfix {0} {1} {2}.*", Path.GetDirectoryName(termsrvPath), testFolder, Path.GetFileNameWithoutExtension(termsrvPath))).WaitForExit();

            // Patch the test file
            Patcher p = new Patcher(Path.Combine(testFolder, Path.GetFileName(termsrvPath)));
            p.patch(textBoxFind.Text, textBoxReplace.Text);

            ServiceController sc = new ServiceController("TermService");
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void buttonPatch_Click(object sender, RoutedEventArgs e)
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            // Patch the real file
            Patcher p = new Patcher();
            p.patch(textBoxFind.Text, textBoxReplace.Text);

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void buttonSetRegistry_Click(object sender, RoutedEventArgs e)
        {
            //RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true);
            // Enable RDP
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryValueKind.DWord);

            // Disable multiple sessions per user
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1, RegistryValueKind.DWord);

            // Disable remote logon for user accounts that are not password protected
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1, RegistryValueKind.DWord);
        }
    }
}
