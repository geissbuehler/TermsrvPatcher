using System;
using System.Linq;
using System.ServiceProcess;
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
        public MainWindow()
        {
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
            scrollviewerMessages.Content = "termsrv.dll version: " + patcher.GetVersion() + Environment.NewLine;
            //checkBoxTestMode.IsChecked = true;
            //checkStatus();
            formInitialized = true;
        }

        private void ButtonPatch_Click(object sender, RoutedEventArgs e)
        {
            Patch();
            CheckStatus();
        }

        private void ButtonUnpatch_Click(object sender, RoutedEventArgs e)
        {
            Unpatch();
            CheckStatus();
        }

        private void ButtonCheckStatus_Click(object sender, RoutedEventArgs e)
        {
            CheckStatus();
        }

        private void CheckStatus()
        {
            switch (patcher.CheckStatus(textBoxFind.Text, textBoxReplace.Text))
            {
                case 1:
                    scrollviewerMessages.Content += "Status: Patched";
                    break;
                case 0:
                    scrollviewerMessages.Content += "Status: Unpatched";
                    break;
                case -1:
                    scrollviewerMessages.Content += "Status: Unkown";
                    break;
            }
            scrollviewerMessages.Content += Environment.NewLine;
        }

        private void Patch()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            if (patcher.CheckStatus(textBoxFind.Text, textBoxReplace.Text) == 0)
            {
                patcher.Patch(textBoxFind.Text, textBoxReplace.Text);
            }

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void Unpatch()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            patcher.Unpatch();

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
