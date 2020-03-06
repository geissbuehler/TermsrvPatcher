using System;
using System.Linq;
using System.ServiceProcess;
using System.ComponentModel;
using System.Windows;
using NetFwTypeLib;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Patcher patcher;
        private bool formInitialized = false;
        private int status = -1;
        private string version = "";
        private readonly BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            InitializeComponent();
            patcher = new Patcher();
            if (patcher.AllowRdp)
            {
                radioButtonEnableRdp.IsChecked = true;
            }
            else
            {
                radioButtonDisableRdp.IsChecked = true;
            }
            if (patcher.AllowMulti)
            {
                radioButtonEnableMulti.IsChecked = true;
            }
            else
            {
                radioButtonDisableMulti.IsChecked = true;
            }
            if (patcher.AllowBlank)
            {
                radioButtonEnableBlank.IsChecked = true;
            }
            else
            {
                radioButtonDisableBlank.IsChecked = true;
            }
            CheckStatus();
            EnableControls();
            formInitialized = true;
        }

        private void DisableControls()
        {
            buttonPatch.IsEnabled = false;
            buttonUnpatch.IsEnabled = false;
            buttonCheckStatus.IsEnabled = false;
            textBoxFind.IsEnabled = false;
            textBoxReplace.IsEnabled = false;
        }

        private void EnableControls()
        {
            switch (status)
            {
                case 1:
                    buttonUnpatch.IsEnabled = true;
                    buttonPatch.IsEnabled = false;
                    break;
                case 0:
                    buttonUnpatch.IsEnabled = false;
                    buttonPatch.IsEnabled = true;
                    break;
                case -1:
                    buttonUnpatch.IsEnabled = false;
                    buttonPatch.IsEnabled = false;
                    break;
            }
            buttonCheckStatus.IsEnabled = true;
            textBoxFind.IsEnabled = true;
            textBoxReplace.IsEnabled = true;
        }

        private void ButtonPatch_Click(object sender, RoutedEventArgs e)
        {
            DisableControls();
            worker.RunWorkerAsync(argument: new object[] { true, textBoxFind.Text, textBoxReplace.Text });
         }

        private void ButtonUnpatch_Click(object sender, RoutedEventArgs e)
        {
            DisableControls();
            worker.RunWorkerAsync(argument: new object[] { false });
        }

        private void ButtonCheckStatus_Click(object sender, RoutedEventArgs e)
        {
            CheckStatus();
        }

        private void AddMessage(string message, bool appendLine = false)
        {
            if ((textBoxMessages.Text.Length) > 0 && (!appendLine))
            {
                textBoxMessages.Text += Environment.NewLine + message;
            }
            else
            {
                textBoxMessages.Text += message;
            }
            textBoxMessages.ScrollToEnd();
        }

        private void CheckStatus()
        {
            version = patcher.GetVersion();
            textBlockVersion.Text = "termsrv.dll version: " + version;
            status = patcher.CheckStatus(textBoxFind.Text, textBoxReplace.Text);
            switch (status)
            {
                case 1:
                    textBlockStatus.Text = "termsrv.dll status: Patched";
                    break;
                case 0:
                    textBlockStatus.Text = "termsrv.dll status: Unpatched";
                    break;
                case -1:
                    textBlockStatus.Text = "termsrv.dll status: Unkown";
                    break;
            }
            if (patcher.BackupAvailable())
            {
                textBlockBackupStatus.Text = "termsrv.dll backup: Available";
            }
            else
            {
                textBlockBackupStatus.Text = "termsrv.dll backup: Not available";
            }
        }

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            buttonTest.IsEnabled = false;
            worker.RunWorkerAsync(argument: true);

            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            /*var foo = firewallPolicy.Rules.OfType<INetFwRule>();
            foreach (dynamic rule in foo)
            {
                dynamic bar = rule.Name;
            }
            INetFwRule firewallRule = firewallPolicy.Rules.OfType<INetFwRule>().Where(x => x.LocalPorts == "3389").FirstOrDefault();
            dynamic baz = firewallRule.Name;*/

            System.Collections.Generic.IEnumerable<INetFwRule> rules;

            /*rules = firewallPolicy.Rules.OfType<INetFwRule>();
            foreach (dynamic rule in rules)
            {
                if (rule.ApplicationName == "C:\\WINDOWS\\system32\\RdpSa.exe")
                {
                    //scrollviewerMessages.Content += rule.Name;
                    //scrollviewerMessages.Content += Environment.NewLine;
                }
                if (rule.LocalPorts == "3389")
                {
                    //scrollviewerMessages.Content += Convert.ToString(rule.Direction);
                    //scrollviewerMessages.Content += Convert.ToString(rule.Profiles);
                    //scrollviewerMessages.Content += rule.Name;
                    //scrollviewerMessages.Content += Environment.NewLine;
                }
                //scrollviewerMessages.Content += rule.serviceName;
                //scrollviewerMessages.Content += Environment.NewLine;
                int bazbaz = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_ALL;
                //rule.Profiles = (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN;
            }*/

            /*rules = firewallPolicy.Rules.OfType<INetFwRule>().Where(x =>
                x.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN //
                // Exclude rules explicitly only for public networks (Windows 7 has a public and a private/domain RDP rule)
                && Convert.ToBoolean(x.Profiles & ((int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN))
                && x.serviceName == "termservice" //
                && x.LocalPorts == "3389" //
            );
            foreach (dynamic rule in rules)
            {
                scrollviewerMessages.Content += rule.Name;
                scrollviewerMessages.Content += Environment.NewLine;
            }

            rules = firewallPolicy.Rules.OfType<INetFwRule>().Where(x =>
                x.Direction == NET_FW_RULE_DIRECTION_.NET_FW_RULE_DIR_IN
                // Exclude rules explicitly only for public networks
                && Convert.ToBoolean(x.Profiles & ((int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE | (int)NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_DOMAIN))
                && x.ApplicationName == "C:\\WINDOWS\\system32\\RdpSa.exe"
            );
            foreach (dynamic rule in rules)
            {
                scrollviewerMessages.Content += rule.Name;
                scrollviewerMessages.Content += Environment.NewLine;
                // Exclude rules explicitly only for public networks (Windows 7 has a public and a private/domain RDP rule)
                // Works!
                //rule.Enabled = false;
            }*/

            // Group "Remotedesktop - RemoteFX" (Windows 7)
            rules = firewallPolicy.Rules.OfType<INetFwRule>().Where(x =>
                x.Grouping == "@FirewallAPI.dll,-28852"
            );
            foreach (dynamic rule in rules)
            {
                AddMessage(rule.Name);
            }

            // Group "Remotedesktop"
            rules = firewallPolicy.Rules.OfType<INetFwRule>().Where(x =>
                x.Grouping == "@FirewallAPI.dll,-28752"
            );
            foreach (dynamic rule in rules)
            {
                AddMessage(rule.Name);
            }
        }

        private void RadioButtonEnableRdp_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowRdp = true;
                patcher.SetFirewall(true);
            }
        }

        private void RadioButtonDisableRdp_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowRdp = false;
                patcher.SetFirewall(false);
            }
        }

        private void RadioButtonDisableMulti_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowMulti = false;
            }
        }

        private void RadioButtonEnableMulti_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowMulti = true;
            }
        }

        private void RadioButtonDisableBlank_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowBlank = false;
            }
        }

        private void RadioButtonEnableBlank_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.AllowBlank = true;
            }
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                worker.ReportProgress(20, new object[] { "Stopping TermService...", false });
                patcher.StopTermService();
                worker.ReportProgress(40, new object[] { " Done", true });
                if (Convert.ToBoolean((e.Argument as object[])[0]))
                {
                    //patch
                    string find = (e.Argument as object[])[1] as string;
                    string replace = (e.Argument as object[])[2] as string;
                    if (patcher.CheckStatus(find, replace) == 0)
                    {
                        worker.ReportProgress(60, new object[] { "Patching termsrv.dll", false });
                        patcher.Patch(find, replace);
                    }
                }
                else
                {
                    //unpatch
                    worker.ReportProgress(60, new object[] { "Restoring termsrv.dll backup", false });
                    patcher.Unpatch();
                }
                worker.ReportProgress(80, new object[] { "Starting TermService...", false });
                patcher.StartTermService();
                worker.ReportProgress(100, new object[] { " Done", true });
            }
            catch (Exception exc)
            {
                worker.ReportProgress(100, new object[] { exc.ToString(), false });
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            AddMessage((e.UserState as object[])[0] as string, Convert.ToBoolean((e.UserState as object[])[1]));
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AddMessage("Old status: " + status);
            CheckStatus();
            AddMessage("New status: " + status);
            EnableControls();
        }
    }
}
