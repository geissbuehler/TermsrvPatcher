using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string path;
        public MainWindow()
        {
            InitializeComponent();
            path = Patcher.termsrvPath;
        }

        private void buttonVersion_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(Patcher.getVersion(path));
        }

        private void buttonChown_Click(object sender, RoutedEventArgs e)
        {
            Patcher.takeOwnership(path, Patcher.getAdministratorsIdentity());
        }

        private void buttonTest_Click(object sender, RoutedEventArgs e)
        {
            /*// Taking ownership
            Process.Start("takeown", @"/a /r /f C:\termsrv\").WaitForExit();
            // Granting Administrators rights
            Process.Start("ICACLS", @"C:\termsrv\ /Grant *S-1-5-32-544:F /t").WaitForExit();

            Process.Start("robocopy.exe", @"/mir /lev:1 /copyall /secfix c:\windows\system32 C:\termsrv termsrv.*").WaitForExit();*/

            patch(@"C:\termsrv\termsrv.dll");
        }

        private void patch(string filePath)
        {
            string backup = filePath + "." + Patcher.getVersion(filePath);
            if (!File.Exists(backup))
            {
                textBlockMessages.Text += "Backup file...";
                File.Copy(filePath, backup);
                textBlockMessages.Text += " OK";
            }
            Patcher.takeOwnership(filePath, Patcher.getAdministratorsIdentity());
            Patcher.setFullControl(filePath, Patcher.getAdministratorsIdentity());
            FileInfo fi = new FileInfo(filePath);
            long l = fi.Length;
            byte b;
            string find = "39 81 3C 06 00 00 0F 84 53 71 02 00";
            string replace = "B8 00 01 00 00 89 81 38 06 00 00 90";
            int? firstHex = null; // handles asterisks at beginning
            List<int> bin = new List<int>();
            foreach (string hex in find.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    if (hex == "*")
                    {
                        bin.Add(-1);
                    }
                    else
                    {
                        if (firstHex == null)
                        {
                            // Position of first non-astersik
                            firstHex = bin.Count;
                        }
                        bin.Add(System.Convert.ToInt32(hex, 16));
                    }
                }
            }
            if (bin.Count == 0)
            {
                // Exception
            }
            if (firstHex == null)
            {
                // Exception here
            }

            List<byte> binReplace = new List<byte>();
            foreach (string hex in replace.Split(' '))
            {
                if (hex != System.String.Empty)
                {
                    binReplace.Add(System.Convert.ToByte(hex, 16));
                }
            }
            if (binReplace.Count == 0)
            {
                // Exception
            }



            byte[] binFile = File.ReadAllBytes(filePath);
            int len = binFile.Length;
            int match = -1;
            for (int i = firstHex.Value; i < len; i++)
            {
                if (binFile[i] == bin[firstHex.Value])
                {
                    if (len - i >= 0) // rethink >= 0
                    {
                        match = i - firstHex.Value;
                        for (int ii = 0; ii < bin.Count; ii++)
                        {
                            if (bin[ii] != -1 && bin[ii] != binFile[i+ii])
                            {
                                match = -1;
                                break;
                            }
                        }
                    }
                }
                if (match > -1)
                {
                    break;
                }
            }
            if (match == -1)
            {
                // Exception
            }

            using (BinaryWriter reader = new BinaryWriter(File.Open(filePath, FileMode.Open)))
            {
                reader.BaseStream.Seek(match, SeekOrigin.Begin);
                reader.Write(binReplace.ToArray());
            }
        }
    }
}
