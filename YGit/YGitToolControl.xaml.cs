using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using YGit.ViewModel;

namespace YGit
{
    /// <summary>
    /// Interaction logic for YGitToolControl.
    /// </summary>
    public partial class YGitToolControl : UserControl
    {
        YGitVM gitVM;

        System.Threading.Timer timer;

        bool isInitialized;
        /// <summary>
        /// Initializes a new instance of the <see cref="YGitToolControl"/> class.
        /// </summary>
        public YGitToolControl()
        {
            this.InitializeComponent();
            var slnPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
            var events = YGitPackage.vsDTE.Solution?.DTE.Events;
            var slnEvents = events?.SolutionEvents;
            gitVM = new YGitVM();
            this.DataContext = gitVM;
            slnEvents.AfterClosing += () => gitVM.GitConf = null;
            slnEvents.Opened += () =>
            {
                gitVM.RepoPath = slnPath;
                gitVM.LoadConf();
            }; 

            timer = new System.Threading.Timer(obj =>
            {
                gitVM.RepoPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
                if (!string.IsNullOrWhiteSpace(gitVM.RepoPath))
                {
                    gitVM.LoadConf();
                    gitVM.LoadCurrentBranche();
                    timer.Dispose();
                }

            }, null, 5000, 5000); 
        }


        /// <summary>
        /// Handles click on the button by displaying a message box.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}' 9999", this.ToString()),
                "YGitTool");
        }
    }
}