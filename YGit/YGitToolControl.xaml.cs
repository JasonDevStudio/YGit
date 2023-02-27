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

        bool isInitialized;
        /// <summary>
        /// Initializes a new instance of the <see cref="YGitToolControl"/> class.
        /// </summary>
        public YGitToolControl()
        {
            this.InitializeComponent();
            this.Loaded += (s,e)=>
            {
                var slnPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
                var events = YGitPackage.vsDTE.Solution?.DTE.Events;
                var slnEvents = events?.SolutionEvents;

                if (!string.IsNullOrWhiteSpace(slnPath))
                {
                    gitVM = new YGitVM(slnPath);
                    this.DataContext = gitVM;
                }
                else
                {
                    gitVM = new YGitVM();
                    gitVM.LoadConf();
                    this.DataContext = gitVM;
                }

                if (slnEvents != null && !isInitialized)
                {
                    slnEvents.Opened += () =>
                    {
                        gitVM.RepoPath = slnPath;
                        gitVM.LoadConf();
                    };

                    slnEvents.AfterClosing += () => gitVM.GitConf = null;
                    isInitialized = true;
                }
            };

            this.IsVisibleChanged += (s, e) =>
            {
                var slnPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
                var events = YGitPackage.vsDTE.Solution?.DTE.Events;
                var slnEvents = events?.SolutionEvents;

                if (!string.IsNullOrWhiteSpace(slnPath))
                {
                    gitVM = new YGitVM(slnPath);
                    this.DataContext = gitVM;
                }
                else
                {
                    gitVM = new YGitVM();
                    gitVM.LoadConf();
                    this.DataContext = gitVM;
                }

                if (slnEvents != null && !isInitialized)
                {
                    slnEvents.Opened += () =>
                    {
                        gitVM.RepoPath = slnPath;
                        gitVM.LoadConf();
                    };

                    slnEvents.AfterClosing += () => gitVM.GitConf = null;
                    isInitialized = true;
                }
            };
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