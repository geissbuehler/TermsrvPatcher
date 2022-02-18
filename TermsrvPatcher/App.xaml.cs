using System.Windows;

namespace TermsrvPatcher
{
    /// <summary>
    /// Interaction logic for "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        public static bool unattended = false;
        void App_Startup(object sender, StartupEventArgs e)
        {
            for (int i = 0; i != e.Args.Length; ++i)
            {
                if (e.Args[i].ToLower() == "/unattended" | e.Args[i].ToLower() == "-unattended")
                {
                    unattended = true;
                }
            }
        }
    }
}
