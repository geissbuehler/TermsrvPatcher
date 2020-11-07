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
        Patcher.Status status = Patcher.Status.Unkown;
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
                case Patcher.Status.Patched:
                    buttonUnpatch.IsEnabled = true;
                    buttonPatch.IsEnabled = false;
                    break;
                case Patcher.Status.Unpatched:
                    buttonUnpatch.IsEnabled = false;
                    buttonPatch.IsEnabled = true;
                    break;
                case Patcher.Status.Unkown:
                    buttonUnpatch.IsEnabled = false;
                    buttonPatch.IsEnabled = false;
                    break;
            }
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
            buttonCheckStatus.IsEnabled = false;
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
            textBlockVersion.Text = "Version: " + version;
            try
            {
                status = (Patcher.Status) patcher.CheckStatus(textBoxFind.Text, textBoxReplace.Text);
            }
            catch (Exception exc)
            {
                AddMessage(exc.ToString());
                status = Patcher.Status.Unkown;
            }
            switch (status)
            {
                case Patcher.Status.Patched:
                    textBlockStatus.Text = "Status: " + Patcher.Status.Patched.ToString();
                    break;
                case Patcher.Status.Unpatched:
                    textBlockStatus.Text = "Status: " + Patcher.Status.Unpatched;
                    break;
                case Patcher.Status.Unkown:
                    textBlockStatus.Text = "Status: " + Patcher.Status.Unkown;
                    break;
            }
            if (patcher.BackupAvailable())
            {
                textBlockBackupStatus.Text = "Backup: Available";
            }
            else
            {
                textBlockBackupStatus.Text = "Backup: Not available";
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

        private void textBoxFind_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (formInitialized)
            {
                buttonCheckStatus.IsEnabled = true;
            }
        }

        private void textBoxReplace_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (formInitialized)
            {
                buttonCheckStatus.IsEnabled = true;
            }
        }
    }
}
