using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Events;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
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
            Dispatcher.VerifyAccess();
            this.InitializeComponent();

            var slnPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
            var events = YGitPackage.vsDTE.Solution?.DTE.Events;
            var slnEvents = events?.SolutionEvents;
            var buildEvents = events?.BuildEvents;
            gitVM = new YGitVM();
            this.DataContext = gitVM;

            buildEvents.OnBuildDone += (s, e) =>
            {
                if (e == vsBuildAction.vsBuildActionBuild)
                {
                    var errors = YGitPackage.vsDTE.ToolWindows.ErrorList;
                    errors.ShowErrors = true;
                    errors.ShowWarnings = false;
                    errors.ShowMessages = false;
                    var errorItems = errors.ErrorItems;

                    for (int i = 0; i < errorItems.Count; i++)
                    {
                        var line = errorItems.Item(i);
                        if (line.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                            gitVM.logger.WriteLine($"Project:{line.Project}, File: {line.FileName},LineNumber:{line.Line}, {line.Description}");
                    }
                }
            };
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
                    gitVM.CommitRefresh();
                    gitVM.ModifiedRefresh();
                }

            }, null, 15000, 30000);

            // 推送前触发事件  触发项目编译
            gitVM.BeforePushEvent = () =>
            {
                Dispatcher.VerifyAccess();
                var iscompiled = this.CompileProjects(YGitPackage.vsDTE.DTE);

                if (iscompiled)
                    gitVM.IsCompiled = iscompiled;
                else
                    MessageBox.Show("代码编译失败，推送已取消，请解决后推送。详细查阅【错误】面板。", "YGitTool", MessageBoxButton.OK, MessageBoxImage.Error);
            };
        }

        /// <summary>
        /// Compiles the project.
        /// </summary>
        /// <param name="dte">The DTE.</param>
        /// <param name="project">the project.</param>
        /// <returns></returns>
        private bool CompileProjects(DTE dte)
        {
            Dispatcher.VerifyAccess();
            var solution = (Solution2)dte.Solution;
            var state = true;

            if (YGitPackage.vsDTE.DTE.Solution.Projects.Count < 1)
                return true;

            solution.SolutionBuild.Clean(true);

            foreach (Project project in YGitPackage.vsDTE.DTE.Solution.Projects)
            {
                if (project == null)
                {
                    state &= true;
                }

                try
                {
                    solution.SolutionBuild.BuildProject(solution.SolutionBuild.ActiveConfiguration.Name, project.UniqueName, true);
                    if (solution.SolutionBuild.LastBuildInfo == 0)
                    {
                        // Build succeeded
                        gitVM.logger.WriteLine($"Succeeded: {project.Name} is build succeeded.");
                        state &= true;
                    }
                    else
                    {
                        // Build failed
                        gitVM.logger.WriteLine($"Error: {project.Name} is build failed.");
                        state &= false;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    gitVM.logger.WriteLine($"Error: {project.Name} is Rebuild failed. \n-----{ex}");
                    state &= false;
                }
            }

            return state;
        }

        /// <summary>
        /// Compiles the project.
        /// </summary>
        /// <param name="dte">The DTE.</param>
        /// <param name="project">the project.</param>
        /// <returns></returns>
        private bool CompileProject(DTE dte, Project project)
        {
            Dispatcher.VerifyAccess();
            Solution2 solution = (Solution2)dte.Solution;
            if (project == null)
            {
                return true;
            }

            try
            {
                if (project.Kind == "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}")
                {
                    gitVM.logger.WriteLine($"None: {project.Name} is a shared project..");
                    return true;
                }

                solution.SolutionBuild.BuildProject(project.Name, project.UniqueName, true);
                if (solution.SolutionBuild.LastBuildInfo == 0)
                {
                    // Build succeeded
                    gitVM.logger.WriteLine($"Succeeded: {project.Name} is build succeeded.");
                    return true;
                }
                else
                {
                    // Build failed
                    gitVM.logger.WriteLine($"Error: {project.Name} is build failed.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                gitVM.logger.WriteLine($"Error: {project.Name} is Rebuild failed. \n-----{ex}");
                return false;
            }


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
                gitVM.logger.WriteLine($"Succeeded: {project.Name} is build succeeded.");
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