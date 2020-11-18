using System;
using System.ComponentModel;
using System.Windows;
using System.Collections.Generic;

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
            radioButtonAutoMode.IsChecked = true;
            CheckStatus(false);
            formInitialized = true;
        }

        private void DisableControls()
        {
            buttonPatch.IsEnabled = false;
            buttonUnpatch.IsEnabled = false;
            textBoxFind.IsEnabled = false;
            textBoxReplace.IsEnabled = false;
            radioButtonAutoMode.IsEnabled = false;
            radioButtonManualMode.IsEnabled = false;
        }

        private void SetControls()
        {
            switch (status)
            {
                case Patcher.Status.Patched:
                    buttonPatch.IsEnabled = false;
                    break;
                case Patcher.Status.Unpatched:
                    buttonPatch.IsEnabled = true;
                    break;
                case Patcher.Status.Unkown:
                    buttonPatch.IsEnabled = false;
                    break;
            }
            if (patcher.BackupAvailable())
            {
                buttonUnpatch.IsEnabled = true;
            }
            else
            {
                buttonUnpatch.IsEnabled = false;
            }
            if (radioButtonAutoMode.IsChecked == true)
            {
                textBoxFind.IsEnabled = false;
                textBoxReplace.IsEnabled = false;
            }
            else
            {
                textBoxFind.IsEnabled = true;
                textBoxReplace.IsEnabled = true;
            }
            radioButtonAutoMode.IsEnabled = true;
            radioButtonManualMode.IsEnabled = true;
        }

        private bool CheckRdpSession()
        {
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                var result = System.Windows.Forms.MessageBox.Show("The current remote desktop session will be disconnected. Continue?", "RDP session detected", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
                if (result == System.Windows.Forms.DialogResult.No)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        private void ButtonPatch_Click(object sender, RoutedEventArgs e)
        {
            if (CheckRdpSession())
            {
                DisableControls();
                worker.RunWorkerAsync(argument: new object[] { true, Patcher.StringsToPatch(textBoxFind.Text, textBoxReplace.Text) });
            }
        }

        private void ButtonUnpatch_Click(object sender, RoutedEventArgs e)
        {
            if (CheckRdpSession())
            {
                DisableControls();
                worker.RunWorkerAsync(argument: new object[] { false });
            }
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

        private void CheckStatus(bool quickCheck)
        {
            if (!quickCheck)
            {
                textBoxMessages.Clear();
            }
            version = patcher.GetVersion();
            bool success = false;
            textBlockVersion.Text = "Version: " + version;
            if (!quickCheck && (radioButtonAutoMode.IsChecked == true))
            {
                List<object> patches = new List<object>();
                try
                {
                    List<object> res = Patcher.ReadPatchfile();
                    patches = (List<object>)res[0];
                    List<string> warnings = (List<string>)res[1];
                    foreach (string warning in warnings)
                    {
                        AddMessage("Warning: " + warning);
                    }
                    success = true;
                }
                catch (System.IO.FileNotFoundException)
                {
                    AddMessage(String.Format("Error: patchfile '{0}' not found, prepare '{0}' or enter patches manually", Patcher.Patchfile));
                }
                catch (Exception exception)
                {
                    AddMessage(String.Format("Error: Reading patchfile '{0}' failed, fix '{0}' or enter patches manually ({1})", Patcher.Patchfile, exception.Message.ToString()));
                }
                if (success)
                {
                    bool nomatch = true;
                    foreach (List<Object> patch in patches)
                    {
                        status = patcher.CheckStatus((List<object>)patch[0]);
                        if (status != Patcher.Status.Unkown)
                        {
                            textBoxFind.Text = "";
                            textBoxReplace.Text = "";
                            foreach (List<object> subpatch in (List<object>)patch[0])
                            {
                                if (textBoxFind.Text != "")
                                {
                                    textBoxFind.Text += " | ";
                                    textBoxReplace.Text += " | ";
                                }
                                textBoxFind.Text += Patcher.IntArrToString((int[])subpatch[0]);
                                textBoxReplace.Text += Patcher.ByteArrToString((byte[])subpatch[1]);
                            }
                            nomatch = false;
                            AddMessage(String.Format("Automatic patching enabled: Matching patch for termsrv.dll in patchfile '{0}' found at line {1}", Patcher.Patchfile, (ulong)patch[1]));
                            break;
                        }
                    }
                    if (nomatch)
                    {
                        AddMessage(String.Format("Error: No matching patch for termsrv.dll found in patchfile '{0}', edit '{0}' or enter patches manually", Patcher.Patchfile));
                    }
                }
                else
                {
                    status = Patcher.Status.Unkown;
                }
            }
            else
            {
                try
                {
                    status = patcher.CheckStatus(Patcher.StringsToPatch(textBoxFind.Text, textBoxReplace.Text));
                    if (!quickCheck)
                    {
                        if (status == Patcher.Status.Unkown)
                        {
                            AddMessage("Error: No match in termsrv.dll found for manual patch patterns");
                        }
                        else
                        {
                            AddMessage("Manual patching enabled: Match in termsrv.dll found for manual patch patterns");
                        }
                    }
                }
                catch (Exception exception)
                {
                    status = Patcher.Status.Unkown;
                    AddMessage(String.Format("Error: {0}", exception.Message.ToString()));
                }
            }
            switch (status)
            {
                case Patcher.Status.Patched:
                    textBlockStatus.Text = "Status: " + Patcher.Status.Patched;
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
            SetControls();
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                worker.ReportProgress(20, new object[] { "Stopping TermService...", false });
                patcher.StopTermService();
                worker.ReportProgress(40, new object[] { " Done", true });
                if (Convert.ToBoolean(((object[])e.Argument)[0]))
                {
                    //patch
                    List<object> patch = (List<object>)((object[])e.Argument)[1];
                    if (patcher.CheckStatus(patch) == Patcher.Status.Unpatched)
                    {
                        worker.ReportProgress(60, new object[] { "Patching termsrv.dll", false });
                        patcher.Patch(patch);
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
            catch (Exception exception)
            {
                worker.ReportProgress(100, new object[] { exception.ToString(), false });
            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            AddMessage((string)((object[])e.UserState)[0], Convert.ToBoolean(((object[])e.UserState)[1]));
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AddMessage("Old status: " + status);
            CheckStatus(true);
            AddMessage("New status: " + status);
            SetControls();
        }

        private void textBoxFind_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (formInitialized && (radioButtonManualMode.IsChecked == true))
            {
                CheckStatus(false);
            }
        }

        private void textBoxReplace_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (formInitialized && (radioButtonManualMode.IsChecked == true))
            {
                CheckStatus(false);
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

        private void radioButtonAutoMode_Checked(object sender, RoutedEventArgs e)
        {
            CheckStatus(false);
        }

        private void radioButtonManualMode_Checked(object sender, RoutedEventArgs e)
        {
            CheckStatus(false);
        }
    }
}
