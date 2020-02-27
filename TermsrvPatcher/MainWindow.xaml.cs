using NetFwTypeLib;
using System;
using System.Linq;
using System.ServiceProcess;
using System.Windows;

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

        private void ButtonSetRegistry_Click(object sender, RoutedEventArgs e)
        {
            SetRegistry();
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

        private void SetRegistry()
        {
            patcher.AllowRdp = radioButtonEnableRdp.IsChecked.GetValueOrDefault();
            patcher.AllowMulti = radioButtonEnableMulti.IsChecked.GetValueOrDefault();
            patcher.AllowBlank = radioButtonEnableBlank.IsChecked.GetValueOrDefault();
        }

        private void ButtonTest_Click(object sender, RoutedEventArgs e)
        {
            INetFwPolicy2 firewallPolicy = (INetFwPolicy2)Activator.CreateInstance(
                Type.GetTypeFromProgID("HNetCfg.FwPolicy2"));

            var foo = firewallPolicy.Rules.OfType<INetFwRule>();
            INetFwRule firewallRule = firewallPolicy.Rules.OfType<INetFwRule>().Where(x => x.LocalPorts == "3389").FirstOrDefault();

        }
    }
}
