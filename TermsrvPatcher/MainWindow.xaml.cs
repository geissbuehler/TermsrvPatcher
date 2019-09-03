using System.Windows;
using System.IO;
using System.Diagnostics;
using System;
using System.ServiceProcess;
using NetFwTypeLib;
using System.Linq;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Patcher patcher;
        public MainWindow()
        {
            InitializeComponent();
            patcher = new Patcher();
            if (patcher.allowRdp)
            {
                radioButtonEnableRdp.IsChecked = true;
            }
            else
            {
                radioButtonDisableRdp.IsChecked = true;
            }
            if (patcher.allowMulti)
            {
                radioButtonEnableMulti.IsChecked = true;
            }
            else
            {
                radioButtonDisableMulti.IsChecked = true;
            }
            if (patcher.allowBlank)
            {
                radioButtonEnableBlank.IsChecked = true;
            }
            else
            {
                radioButtonDisableBlank.IsChecked = true;
            }
            checkBoxTestMode.IsChecked = true;
            //checkStatus();
        }

        private void buttonPatch_Click(object sender, RoutedEventArgs e)
        {
            patch();
            checkStatus();
        }

        private void buttonSetRegistry_Click(object sender, RoutedEventArgs e)
        {
            setRegistry();
        }

        private void buttonCheckStatus_Click(object sender, RoutedEventArgs e)
        {
            checkStatus();
        }

        private void checkStatus()
        {
            switch (patcher.checkStatus(textBoxFind.Text, textBoxReplace.Text))
            {
                case 1:
                    textBlockMessages.Text += "Status: Patched";
                    break;
                case 0:
                    textBlockMessages.Text += "Status: Unpatched";
                    break;
                case -1:
                    textBlockMessages.Text += "Status: Unkown";
                    break;
            }
            textBlockMessages.Text += Environment.NewLine;
        }

        private void patch()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            if (patcher.checkStatus(textBoxFind.Text, textBoxReplace.Text) == 0)
            {
                patcher.patch(textBoxFind.Text, textBoxReplace.Text);
            }

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void setRegistry()
        {
            bool blank = patcher.allowBlank;
            /*//RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true);
            // Enable RDP
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryValueKind.DWord);

            // Disable multiple sessions per user
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1, RegistryValueKind.DWord);

            // Disable remote logon for user accounts that are not password protected
            Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Lsa", "LimitBlankPasswordUse", 1, RegistryValueKind.DWord);*/
        }

        private void checkBoxTestMode_Checked(object sender, RoutedEventArgs e)
        {
            /// <summary>
            /// Prepares a copy of termsrv.dll for testing the processing of taking ownership, getting full control and patching the file.
            /// Uses robocopy to prepare a copy of C:\Windows\system32\termsrv.dll in the folder C:\termsrv with exact the same persmissions and ownership for the file and the containing folder.
            /// </summary>

            //RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server", true);
            // Enable RDP
            //Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fDenyTSConnections", 0, RegistryValueKind.DWord);

            // Disable multiple sessions per user
            //Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Terminal Server", "fSingleSessionPerUser", 1, RegistryValueKind.DWord);

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
            patcher = new Patcher(Path.Combine(testFolder, Path.GetFileName(termsrvPath)));
            //patcher.patch(textBoxFind.Text, textBoxReplace.Text);

            /*ServiceController sc = new ServiceController("TermService");
            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);*/
            checkStatus();
        }

        private void checkBoxTestMode_Unchecked(object sender, RoutedEventArgs e)
        {
            patcher = new Patcher();
            checkStatus();
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            var foo = firewallPolicy.Rules.OfType<INetFwRule>();
            INetFwRule firewallRule = firewallPolicy.Rules.OfType<INetFwRule>().Where(x => x.LocalPorts == "3389").FirstOrDefault();

        }
    }
}
