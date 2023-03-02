using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using YGit.ViewModel;
using Constants = EnvDTE.Constants;

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
                }

            }, null, 5000, 10000);

            // 推送前触发事件  触发项目编译
            gitVM.BeforePushEvent = () =>
            {
                var iscompiled = true;
                var projects = GetEditedProjects(YGitPackage.vsDTE.DTE);

                foreach (Project project in YGitPackage.vsDTE.DTE.Solution.Projects)
                    iscompiled &= CompileProject(YGitPackage.vsDTE.DTE, project);

                if (gitVM.IsCompiled)
                    gitVM.IsCompiled = iscompiled;
                else
                    MessageBox.Show("代码编译失败，本次代码推送已取消，请解决错误后再次推送。详细信息请查阅【YGitTool】输出。", "YGitTool", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        /// <summary>
        /// Compiles the project.
        /// </summary>
        /// <param name="dte">The DTE.</param>
        /// <param name="project">the project.</param>
        /// <returns></returns>
        private bool CompileProject(DTE dte, Project project)
        {
            Solution2 solution = (Solution2)dte.Solution;

            if (project == null)
            {
                return true;
            }

            solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);

            EnvDTE80.DTE2 dte2 = (EnvDTE80.DTE2)dte;
            var panes = dte2.ToolWindows.OutputWindow.OutputWindowPanes;
            OutputWindowPane buildOutputPane;

            foreach (EnvDTE.OutputWindowPane pane in panes)
            {
                if (pane.Name.Contains("Build"))
                {
                    buildOutputPane = pane;
                    break;
                }
            }
              
            if (solution.SolutionBuild.LastBuildInfo == 0)
            {
                // Build succeeded
                gitVM.logger.WriteLine($"Error: {project.Name} is build succeeded.");
                return true;
            }
            else
            {
                // Build failed
                gitVM.logger.WriteLine($"Error: {project.Name} is build failed.");
                return false;
            }
        }

        /// <summary>
        /// Gets the edited projects.
        /// </summary>
        /// <param name="dte">The DTE.</param>
        /// <returns></returns>
        private List<string> GetEditedProjects(DTE dte)
        {
            Solution solution = dte.Solution;
            List<string> editedProjects = new List<string>();

            foreach (Project project in solution.Projects)
            {
                foreach (ProjectItem item in project.ProjectItems)
                {
                    if (item.IsDirty)
                    {
                        if (!editedProjects.Contains(project.Name))
                            editedProjects.Add(project.Name);

                        gitVM.logger.WriteLine($"Project:[{project.Name}], File:[{item.Name}] is modified.");
                    }
                    else
                    {
                        gitVM.logger.WriteLine($"Project:[{project.Name}], File:[{item.Name}] is not modified.");
                    }
                }
            }

            return editedProjects;
        } 
    }
}