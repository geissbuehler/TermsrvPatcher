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
            scrollviewerMessages.Content = "termsrv.dll version: " + patcher.getVersion() + Environment.NewLine;
            //checkBoxTestMode.IsChecked = true;
            //checkStatus();
        }

        private void buttonPatch_Click(object sender, RoutedEventArgs e)
        {
            patch();
            checkStatus();
        }

        private void ButtonUnpatch_Click(object sender, RoutedEventArgs e)
        {
            unpatch();
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

        private void unpatch()
        {
            ServiceController sc = new ServiceController("TermService");

            if (sc.Status == ServiceControllerStatus.Running)
            {
                sc.Stop();
            }
            sc.WaitForStatus(ServiceControllerStatus.Stopped);

            patcher.unpatch();

            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running);
        }

        private void setRegistry()
        {
            patcher.allowRdp = radioButtonEnableRdp.IsChecked.GetValueOrDefault();
            patcher.allowMulti = radioButtonEnableMulti.IsChecked.GetValueOrDefault();
            patcher.allowBlank = radioButtonEnableBlank.IsChecked.GetValueOrDefault();
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
