using System;
using System.ComponentModel;
using System.Windows;
using System.Collections.Generic;
using static TermsrvPatcher.Patcher.Status;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Patcher patcher;
        private bool formInitialized = false;
        Patcher.Status status = Unkown;
        private string version = "";
        List<object> patches = new List<object>();
        private bool readfileSuccess = false;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private readonly BackgroundWorker exitTimer = new BackgroundWorker();

        public MainWindow()
        {
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.ProgressChanged += worker_ProgressChanged;
            exitTimer.DoWork += exitTimer_DoWork;
            exitTimer.RunWorkerCompleted += exitTimer_RunWorkerCompleted;
            InitializeComponent();
            // Get Assemblyversion
            string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            // Remove last digit of Assemblyversion, except it is not equal to 0
            version = System.Text.RegularExpressions.Regex.Replace(version, @"(\d\.\d\.\d)(\.0$)", "$1");
            // Update the link text
            System.Windows.Documents.Run link = (System.Windows.Documents.Run)HyperlinkPatcherVersion.Inlines.FirstInline;
            link.Text = String.Format("Patcher version {0}", version);
            patcher = new Patcher();
            if (patcher.AllowRdp)
            {
                radioButtonEnableRdp.IsChecked = true;
            }
            else
            {
                radioButtonDisableRdp.IsChecked = true;
            }
            if (patcher.EnableNla)
            {
                checkBoxNla.IsChecked = true;
            }
            else
            {
                checkBoxNla.IsChecked = false;
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
            if (App.unattended)
            {
                AddMessage("---Running in unattended mode---");
                if (status == Unpatched)
                {
                    Patch();
                }
                else
                {
                    if (status == Patched)
                    {
                        AddMessage("Already patched, nothing to do");
                    }
                    else if (status == Unkown)
                    {
                        AddMessage("Patch status unkown, nothing to do");
                    }
                    AddMessage("---Exiting in 10 seconds---");
                    exitTimer.RunWorkerAsync();
                }
            }
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
            if (App.unattended)
            {
                buttonPatch.IsEnabled = false;
                buttonUnpatch.IsEnabled = false;
                textBoxFind.IsEnabled = false;
                textBoxReplace.IsEnabled = false;
                radioButtonAutoMode.IsEnabled = false;
                radioButtonManualMode.IsEnabled = false;
                radioButtonEnableRdp.IsEnabled = false;
                radioButtonDisableRdp.IsEnabled = false;
                checkBoxNla.IsEnabled = false;
                radioButtonEnableMulti.IsEnabled = false;
                radioButtonDisableMulti.IsEnabled = false;
                radioButtonEnableBlank.IsEnabled = false;
                radioButtonDisableBlank.IsEnabled = false;
            }
            else
            {
                switch (status)
                {
                    case Patched:
                        buttonPatch.IsEnabled = false;
                        break;
                    case Unpatched:
                        buttonPatch.IsEnabled = true;
                        break;
                    case Unkown:
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
        }

        private bool CheckRdpSession(string text)
        {
            if (System.Windows.Forms.SystemInformation.TerminalServerSession)
            {
                var result = System.Windows.Forms.MessageBox.Show(text, "RDP session detected", System.Windows.Forms.MessageBoxButtons.YesNo, System.Windows.Forms.MessageBoxIcon.Question);
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

        private void Patch()
        {
            worker.RunWorkerAsync(argument: new object[] { true, Patcher.StringsToPatch(textBoxFind.Text, textBoxReplace.Text) });
        }

        private void ButtonPatch_Click(object sender, RoutedEventArgs e)
        {
            // Stoppig: 3 * ServiceTimeout + Starting: 1 * ServiceTimeout
            if (CheckRdpSession($"The current remote desktop session will be disconnected and might remain unreachable for more than {4 * Patcher.ServiceTimeout} seconds. Continue?"))
            {
                DisableControls();
                Patch();
            }
        }

        private void ButtonUnpatch_Click(object sender, RoutedEventArgs e)
        {
            // Stoppig: 3 * ServiceTimeout + Starting: 1 * ServiceTimeout
            if (CheckRdpSession($"The current remote desktop session will be disconnected and might remain unreachable for more than {4 * Patcher.ServiceTimeout} seconds. Continue?"))
            {
                DisableControls();
                worker.RunWorkerAsync(argument: new object[] { false });
            }
        }

        private void AddMessage(string message, bool appendLine = false)
        {
            // The message is appended to the existing line if appendLine = true
            // The last line never ends with a new line
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
            textBlockVersion.Text = "Version: " + version;
            if (radioButtonAutoMode.IsChecked == true)
            {
                if (!quickCheck)
                {
                    // Read patches from file

                    try
                    {
                        List<object> res = patcher.ReadPatchfile();
                        patches = (List<object>)res[0];
                        List<string> warnings = (List<string>)res[1];
                        foreach (string warning in warnings)
                        {
                            AddMessage("Warning: " + warning);
                        }
                        readfileSuccess = true;
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        readfileSuccess = false;
                        AddMessage(String.Format("Error: patchfile '{0}' not found, prepare '{0}' or enter patches manually", patcher.Patchfile));
                    }
                    catch (Exception exception)
                    {
                        readfileSuccess = false;
                        AddMessage(String.Format("Error: Reading patchfile '{0}' failed, fix '{0}' or enter patches manually ({1})", patcher.Patchfile, exception.Message.ToString()));
                    }
                }

                List<object> matchingPatch = new List<object>();
                ulong patchedMatches = 0;
                ulong unpatchedMatches = 0;
                foreach (List<Object> patch in patches)
                {
                    Patcher.Status patchStatus = patcher.CheckStatus((List<object>)patch[0]);
                    if (patchStatus != Unkown)
                    {
                        if (patchStatus == Patched)
                        {
                            patchedMatches++;
                            AddMessage(String.Format("Patch on line {0} in '{1}' indicates patched termsrv.dll", (ulong)patch[1], patcher.Patchfile));
                        }
                        else
                        {
                            // Remember the patch to use it for patching as long as it remains the only match
                            matchingPatch = patch;
                            unpatchedMatches++;
                            AddMessage(String.Format("Patch on line {0} in '{1}' indicates unpatched termsrv.dll", (ulong)patch[1], patcher.Patchfile));
                        }
                    }
                }
                textBoxFind.Text = "";
                textBoxReplace.Text = "";
                if (readfileSuccess)
                {
                    // Do not complain here again for missing patches if patch file reading skipped or failed (failed reading already shows other warnings)

                    if (unpatchedMatches == 0 && patchedMatches == 0)
                    {
                        status = Unkown;
                        AddMessage(String.Format("Error: No matching patch for termsrv.dll found in patchfile '{0}', edit '{0}' or enter patches manually", patcher.Patchfile));
                    }
                    else if (unpatchedMatches == 1 && patchedMatches == 0 )
                    {
                        // A single match for the unpatched status allows to patch termsrv.dll

                        status = Unpatched;
                        AddMessage(String.Format("Automatic patching enabled: Matching patch for termsrv.dll in patchfile '{0}' found on line {1}", patcher.Patchfile, (ulong)matchingPatch[1]));
                        foreach (List<object> subpatch in (List<object>)matchingPatch[0])
                        {
                            if (textBoxFind.Text != "")
                            {
                                textBoxFind.Text += " | ";
                                textBoxReplace.Text += " | ";
                            }
                            textBoxFind.Text += Patcher.IntArrToString((int[])subpatch[0]);
                            textBoxReplace.Text += Patcher.ByteArrToString((byte[])subpatch[1]);
                        }
                    }
                    else if (unpatchedMatches > 1 && patchedMatches == 0)
                    {
                        // More than one match for unpatched status

                        status = Unkown;
                        AddMessage(String.Format("Error: Multiple patches for termsrv.dll found in patchfile '{0}', edit '{0}' or enter patches manually", patcher.Patchfile));
                    }
                    else if (unpatchedMatches == 0 && patchedMatches > 0)
                    {
                        // One or more matches for the patched status is fine for the various Windows 10 termsrv.dll versions

                        status = Patched;
                    }
                    else if (unpatchedMatches > 0 && patchedMatches > 0)
                    {
                        status = Unkown;
                        AddMessage(String.Format("Error: Contradicting patches for termsrv.dll found in patchfile '{0}', edit '{0}' or enter patches manually", patcher.Patchfile));
                    }
                }
                else
                {
                    status = Unkown;
                }
            }
            else
            {
                try
                {
                    status = patcher.CheckStatus(Patcher.StringsToPatch(textBoxFind.Text, textBoxReplace.Text));
                    if (status == Unkown)
                    {
                        AddMessage("Error: No match in termsrv.dll found for manual patch patterns");
                    }
                    else
                    if (!quickCheck)
                    {
                        {
                            AddMessage("Manual patching enabled: Match in termsrv.dll found for manual patch patterns");
                        }
                    }
                }
                catch (Exception exception)
                {
                    status = Unkown;
                    AddMessage(String.Format("Error: {0}", exception.Message.ToString()));
                }
            }
            switch (status)
            {
                case Patched:
                    textBlockStatus.Text = "Status: " + Patched;
                    break;
                case Unpatched:
                    textBlockStatus.Text = "Status: " + Unpatched;
                    break;
                case Unkown:
                    textBlockStatus.Text = "Status: " + Unkown;
                    break;
            }
            if (patcher.BackupAvailable())
            {
                textBlockBackupStatus.Text = "Backup: Available";
                AddMessage($"Backup: {patcher.GetBackupPath()}");
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
                worker.ReportProgress(20, new object[] { $"Stopping TermService (might take up to {3 * Patcher.ServiceTimeout} seconds)...", false });
                patcher.StopTermService();
                worker.ReportProgress(40, new object[] { " Done", true });
                if (Convert.ToBoolean(((object[])e.Argument)[0]))
                {
                    //patch
                    List<object> patch = (List<object>)((object[])e.Argument)[1];
                    if (patcher.CheckStatus(patch) == Unpatched)
                    {
                        worker.ReportProgress(60, new object[] { "Patching termsrv.dll...", false });
                        patcher.Patch(patch);
                        worker.ReportProgress(70, new object[] { " Done", true });
                    }
                }
                else
                {
                    //unpatch
                    worker.ReportProgress(60, new object[] { "Restoring termsrv.dll backup...", false });
                    patcher.Unpatch();
                    worker.ReportProgress(70, new object[] { " Done", true });
                }
                worker.ReportProgress(80, new object[] { $"Starting TermService (might take up to {Patcher.ServiceTimeout} seconds)...", false });
                try
                {
                    patcher.StartTermService();
                }
                catch (System.ServiceProcess.TimeoutException)
                {
                    worker.ReportProgress(100, new object[] { " Failed", true });
                    worker.ReportProgress(100, new object[] { "Starting TermService timed out", false });
                }
                worker.ReportProgress(100, new object[] { " Done", true });
            }
            catch (ServiceStopException)
            {
                // Unable to stop the service
                worker.ReportProgress(100, new object[] { " Failed", true });
                worker.ReportProgress(100, new object[] { "Stop the Remote Desktop service manually and try again", false });
            }
            // HResult = 32 -> ERROR_SHARING_VIOLATION -> File in use by another process
            catch (System.IO.IOException exception) when ((exception.HResult & 0x0000FFFF) == 32)
            {
                worker.ReportProgress(100, new object[] { " Failed", true });
                worker.ReportProgress(100, new object[] { $"{patcher.TermsrvPath} is in use by another process, stop the Remote Desktop service manually and make sure no process is locking the file and try again", false });
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
            if (App.unattended)
            {
                AddMessage("---Exiting in 10 seconds---");
                exitTimer.RunWorkerAsync();
            }
        }

        private void exitTimer_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Threading.Thread.Sleep(10000);
        }

        private void exitTimer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Application.Current.Shutdown();
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
                if (CheckRdpSession("The current remote desktop session will be disconnected and the system will no more be accessible via remote desktop. Continue?"))
                {
                    patcher.AllowRdp = false;
                    patcher.SetFirewall(false);
                }
                else
                {
                    radioButtonEnableRdp.IsChecked = true;
                }
            }
        }

        private void checkBoxNla_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.EnableNla = true;
            }
        }

        private void checkBoxNla_Unchecked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                patcher.EnableNla = false;
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
            if (formInitialized)
            {
                CheckStatus(false);
            }
        }

        private void radioButtonManualMode_Checked(object sender, RoutedEventArgs e)
        {
            if (formInitialized)
            {
                CheckStatus(false);
            }
        }

        private void HyperlinkPatcherVersion_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
