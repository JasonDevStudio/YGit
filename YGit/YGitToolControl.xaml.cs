using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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

        DispatcherTimer timer;

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

            buildEvents.OnBuildDone += BuildEvents_OnBuildDone;
            slnEvents.AfterClosing += () => gitVM.GitConf = null;
            slnEvents.Opened += SlnEvents_Opened;

            timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(5), IsEnabled = true };
            timer.Tick += Timer_Tick;
            timer.Start();

            // 推送前触发事件  触发项目编译
            gitVM.BeforePushEvent = BeforePush;
            gitVM.GitConfigChangedEvent = GitConfigChanged;
        }

        /// <summary>
        /// Git Config Changed 事件
        /// </summary>
        private void GitConfigChanged()
        {
            Dispatcher.VerifyAccess();

            if (YGitPackage.vsDTE.DTE.Solution?.FullName != gitVM.GitConf?.RootPath)
            {
                var uiShell = Package.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;
                if (MessageBox.Show("检测到当前VS打开的文件夹与YGit.GitConf的路径不一致，是否打开GitConf的路径？", "YGitTool", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    YGitPackage.vsDTE.DTE.ExecuteCommand("File.OpenFolder", gitVM.GitConf.RootPath);
                }
            }
        }

        /// <summary>
        /// 代码推送前事件
        /// </summary>
        private void BeforePush()
        {
            Dispatcher.VerifyAccess();
            var iscompiled = this.CompileProjects(YGitPackage.vsDTE.DTE);

            if (iscompiled)
                gitVM.IsCompiled = iscompiled;
            else
                MessageBox.Show("代码编译失败，推送已取消，请解决后推送。详细查阅【错误】面板。", "YGitTool", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// 解决方案文件夹打开后事件
        /// </summary>
        private void SlnEvents_Opened()
        {
            try
            {
                gitVM.RepoPath = YGitPackage.vsDTE.DTE.Solution?.FullName;
            }
            catch (Exception ex)
            {
                gitVM.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// 项目编译完成事件
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="Action"></param>
        private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            try
            {
                if (Action == vsBuildAction.vsBuildActionBuild)
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
            }
            catch (Exception ex)
            {
                gitVM.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// 定时器执行任务
        /// </summary>
        /// <param name="sender">sender</param>
        /// <param name="e">e</param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                gitVM.LoadConf();
                gitVM.LoadBranches(gitVM.GitConf);
                gitVM.LoadCurrentBranche();
                gitVM.CommitRefresh();
                gitVM.ModifiedRefresh();
            }
            catch (Exception ex)
            {
                gitVM.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
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
    }
}