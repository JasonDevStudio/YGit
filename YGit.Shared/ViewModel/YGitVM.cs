using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using YGit.Common;
using YGit.Model;
using Path = System.IO.Path;

namespace YGit.ViewModel
{
    internal class YGitVM : ObservableObject
    {
        private YGitConf gitConf;
        private YGitConfs gitConfs;
        private ProgressHandler progressHandler;
        private PushStatusErrorHandler pushErrorHandler;
        private CheckoutNotifyHandler checkoutNotifyHandler;
        private CheckoutProgressHandler checkoutProgressHandler;
        private FetchOptions fetchOpts;
        private MergeOptions mergeOpts;
        private PushOptions pushOpts;
        private CheckoutOptions checkoutOpts;
        private CloneOptions cloneOptions;
        private Signature signature;
        private string cmodule;
        private string cmsg;
        private string repoName;
        private string repoPath;
        private string sourceMergeBranch;
        private string checkoutBranch;
        private string checkoutRemoteBranch;
        private string currentBranch;
        private string currentRemoteBranch;
        private bool _initialized = false;
        private bool iscompiled = false;
        private int commitCount;
        private int modifiedCount;
        private ObservableCollection<string> branches = new ObservableCollection<string>();
        private ObservableCollection<string> remoteBranches = new ObservableCollection<string>();
        private ObservableCollection<YGitStatus> changes = new ObservableCollection<YGitStatus>();

        public ILogger logger => GlobaService.GetService<ILogger>();

        /// <summary>
        /// Initializes a new instance of the <see cref="YGitVM"/> class.
        /// </summary>
        public YGitVM()
        {
            this.Initialize();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="YGitVM"/> class.
        /// </summary>
        public YGitVM(string path) : this()
        {
            this.repoPath = path;
            this.LoadConf();
        }

        /// <summary>
        /// Gets or sets the git conf.
        /// </summary>
        /// <value>
        /// The git conf.
        /// </value>
        public YGitConf GitConf
        {
            get => this.gitConf;
            set
            {
                if (value != this.gitConf)
                {
                    this.SetProperty(ref this.gitConf, value);
                    this.OnGitConfChanged(value);
                    this.LoadBranches(value);
                    this.LoadCurrentBranche();
                    this.CommitRefresh();
                    this.ModifiedRefresh();
                    this.GitConfigChangedEvent?.Invoke();
                    this.logger.WriteLine($"Repo Conf is changed, current select conf : {value?.Name}");
                }
            }
        }

        /// <summary>
        /// Gets or sets the git confs.
        /// </summary>
        /// <value>
        /// The git confs.
        /// </value>
        public YGitConfs GitConfs { get => this.gitConfs; set => this.SetProperty(ref this.gitConfs, value); }

        /// <summary>
        /// Branches
        /// </summary>
        public ObservableCollection<string> Branches { get => this.branches; set => this.SetProperty(ref this.branches, value); }

        /// <summary>
        /// RemoteBranchs
        /// </summary>
        public ObservableCollection<string> RemoteBranchs { get => this.remoteBranches; set => this.SetProperty(ref this.remoteBranches, value); }

        /// <summary>
        /// 被更改的集合
        /// </summary>
        public ObservableCollection<YGitStatus> Changes { get => this.changes; set => this.SetProperty(ref this.changes, value); }

        /// <summary>
        /// Gets or sets the name of the repo.
        /// </summary>
        /// <value>
        /// The name of the repo.
        /// </value>
        public string RepoName { get => this.repoName; set => this.SetProperty(ref this.repoName, value); }

        /// <summary>
        /// Gets or sets the repo path.
        /// </summary>
        /// <value>
        /// The repo path.
        /// </value>
        public string RepoPath { get => this.repoPath; set => this.SetProperty(ref this.repoPath, value); }

        /// <summary>
        /// Gets or sets the c module.
        /// </summary>
        /// <value>
        /// The c module.
        /// </value>
        public string CModule { get => this.cmodule; set => this.SetProperty(ref this.cmodule, value); }

        /// <summary>
        /// Gets or sets the c MSG.
        /// </summary>
        /// <value>
        /// The c MSG.
        /// </value>
        public string CMsg { get => this.cmsg; set => this.SetProperty(ref this.cmsg, value); }

        /// <summary>
        /// Gets or sets the source merge branch.
        /// </summary>
        /// <value>
        /// The source merge branch.
        /// </value>
        public string SourceMergeBranch { get => this.sourceMergeBranch; set => this.SetProperty(ref this.sourceMergeBranch, value); }

        /// <summary>
        /// Gets or sets the current branch.
        /// </summary>
        /// <value>
        /// The current branch.
        /// </value>
        public string CurrentBranch { get => this.currentBranch; set => this.SetProperty(ref this.currentBranch, value); }

        /// <summary>
        /// Gets or sets the current remote branch.
        /// </summary>
        /// <value>
        /// The current remote branch.
        /// </value>
        public string CurrentRemoteBranch { get => this.currentRemoteBranch; set => this.SetProperty(ref this.currentRemoteBranch, value); }

        /// <summary>
        /// Gets or sets the target checkout branch.
        /// </summary>
        /// <value>
        /// The target checkout branch.
        /// </value>
        public string CheckoutBranch { get => this.checkoutBranch; set => this.SetProperty(ref this.checkoutBranch, value); }

        /// <summary>
        /// Gets or sets the checkout remote branch.
        /// </summary>
        /// <value>
        /// The checkout remote branch.
        /// </value>
        public string CheckoutRemoteBranch
        {
            get => this.checkoutRemoteBranch;
            set
            {
                this.SetProperty(ref this.checkoutRemoteBranch, value);
                this.CheckoutBranch = value?.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)?.LastOrDefault();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is compiled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is compiled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompiled { get => this.iscompiled; set => this.SetProperty(ref this.iscompiled, value); }

        /// <summary>
        /// 提交数量
        /// </summary>
        public int CommitCount { get => this.commitCount; set => this.SetProperty(ref this.commitCount, value); }

        /// <summary>
        /// 文件修改数量
        /// </summary>
        public int ModifiedCount { get => this.modifiedCount; set => this.SetProperty(ref this.modifiedCount, value); }

        /// <summary>
        /// Gets or sets the clone command.
        /// </summary>
        /// <value>
        /// The clone command.
        /// </value>
        public ICommand CloneCmd => new RelayCommand(Clone);

        /// <summary>
        /// Gets or sets the pull command.
        /// </summary>
        /// <value>
        /// The pull command.
        /// </value>
        public ICommand PullCmd => new RelayCommand(Pull);

        /// <summary>
        /// Gets or sets the fetch command.
        /// </summary>
        /// <value>
        /// The fetch command.
        /// </value>
        public ICommand FetchCmd => new RelayCommand(Fetch);

        /// <summary>
        /// Gets or sets the merge command.
        /// </summary>
        /// <value>
        /// The merge command.
        /// </value>
        public ICommand MergeCmd => new RelayCommand(Merge);

        /// <summary>
        /// Gets or sets the push command.
        /// </summary>
        /// <value>
        /// The push command.
        /// </value>
        public ICommand PushCmd => new RelayCommand(Push);

        /// <summary>
        /// Gets or sets the commit command.
        /// </summary>
        /// <value>
        /// The commit command.
        /// </value>
        public ICommand CommitCmd => new RelayCommand(Commit);

        /// <summary>
        /// Gets the checkout command.
        /// </summary>
        /// <value>
        /// The checkout command.
        /// </value>
        public ICommand CheckoutCmd => new RelayCommand(Checkout);

        /// <summary>
        /// Gets or sets the save conf command.
        /// </summary>
        /// <value>
        /// The save conf command.
        /// </value>
        public ICommand SaveConfCmd => new RelayCommand(SaveConf);

        /// <summary>
        /// Gets or sets the save conf command.
        /// </summary>
        /// <value>
        /// The save conf command.
        /// </value>
        public ICommand SaveConfsCmd => new RelayCommand(SaveConfs);

        /// <summary>
        /// Gets the load conf command.
        /// </summary>
        /// <value>
        /// The load conf command.
        /// </value>
        public ICommand LoadConfCmd => new RelayCommand(LoadConf);

        /// <summary>
        /// Gets or sets the before event.
        /// </summary>
        /// <value>
        /// The before event.
        /// </value>
        public Action BeforePushEvent { get; set; }

        /// <summary>
        /// 加载Config之后触发事件
        /// </summary>
        public Action GitConfigChangedEvent { get; set; }

        /// <summary>
        /// Initializes the specified currpath.
        /// </summary> 
        public void Initialize()
        {
            if (!_initialized)
            {
                progressHandler = new ProgressHandler(msg => logger.WriteLine(msg));
                pushErrorHandler = new PushStatusErrorHandler(error => logger.WriteLine($"{error.Message} {error.Reference}"));
                checkoutNotifyHandler = new CheckoutNotifyHandler(CheckoutNotify);
                checkoutProgressHandler = new CheckoutProgressHandler((title, completedSteps, totalSteps) => logger.WriteLine($"Progress update: {title} ({completedSteps} of {totalSteps} completed)."));
                fetchOpts = new FetchOptions { Prune = true, OnProgress = progressHandler };
                mergeOpts = new MergeOptions() { CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict | CheckoutNotifyFlags.None | CheckoutNotifyFlags.Updated, OnCheckoutNotify = checkoutNotifyHandler, OnCheckoutProgress = checkoutProgressHandler };

                checkoutOpts = new CheckoutOptions()
                {
                    CheckoutNotifyFlags = CheckoutNotifyFlags.Conflict | CheckoutNotifyFlags.None | CheckoutNotifyFlags.Updated,
                    OnCheckoutNotify = checkoutNotifyHandler,
                    CheckoutModifiers = CheckoutModifiers.Force,
                    OnCheckoutProgress = checkoutProgressHandler,
                };

                this._initialized = true;
            }
        }

        /// <summary>
        /// Clones the asynchronous.
        /// </summary>
        public void Clone()
        {
            try
            {
                this.repoPath = null;
                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf != null)
                {
                    if (this.GitConf.OneConf != null)
                        this.CloneModule(this.GitConf.OneConf);

                    if (this.GitConf.TwoConf != null)
                        this.CloneModule(this.GitConf.TwoConf);

                    if (this.GitConf.ThirdConf != null)
                        this.CloneModule(this.GitConf.ThirdConf);

                    this.LoadBranches(this.GitConf);
                    this.LoadCurrentBranche();
                    this.CommitRefresh();
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Pulls the asynchronous.
        /// </summary>
        public void Pull()
        {
            try
            {
                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf.OneConf != null)
                    this.PullModule(this.GitConf.OneConf);

                if (this.GitConf.TwoConf != null)
                    this.PullModule(this.GitConf.TwoConf);

                if (this.GitConf.ThirdConf != null)
                    this.PullModule(this.GitConf.ThirdConf);

                logger.WriteLine($"Pull repo [{this.GitConf.Name}] end.");

                this.CommitRefresh();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// fetch the asynchronous.
        /// </summary>
        public void Fetch()
        {
            try
            {
                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf.OneConf != null)
                    this.FetchModule(this.GitConf.OneConf);

                if (this.GitConf.TwoConf != null)
                    this.FetchModule(this.GitConf.TwoConf);

                if (this.GitConf.ThirdConf != null)
                    this.FetchModule(this.GitConf.ThirdConf);

                logger.WriteLine($"Fetch repo [{this.GitConf.Name}] end.");

                this.CommitRefresh();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Commits the asynchronous.
        /// </summary>
        public void Commit()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.CModule))
                    throw new ArgumentNullException(nameof(this.CModule));

                if (string.IsNullOrWhiteSpace(this.CMsg))
                    throw new ArgumentNullException(nameof(this.CMsg));

                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                var message = $"[{this.CModule}] {this.CMsg}";
                if (this.GitConf.OneConf != null)
                    this.CommitModule(this.GitConf.OneConf, message);

                if (this.GitConf.TwoConf != null)
                    this.CommitModule(this.GitConf.TwoConf, message);

                if (this.GitConf.ThirdConf != null)
                    this.CommitModule(this.GitConf.ThirdConf, message);

                this.CModule = null;
                this.CMsg = null;

                this.CommitRefresh();
                this.ModifiedRefresh();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Checkouts the asynchronous.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// CheckoutRemoteBranch
        /// or
        /// CheckoutBranch
        /// </exception>
        public void Checkout()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.CheckoutRemoteBranch))
                    throw new ArgumentNullException(nameof(this.CheckoutRemoteBranch));

                if (string.IsNullOrWhiteSpace(this.CheckoutBranch))
                    throw new ArgumentNullException(nameof(this.CheckoutBranch));

                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf.OneConf != null)
                    this.CheckoutModule(this.GitConf.OneConf);

                if (this.GitConf.TwoConf != null)
                    this.CheckoutModule(this.GitConf.TwoConf);

                if (this.GitConf.ThirdConf != null)
                    this.CheckoutModule(this.GitConf.ThirdConf);

                this.GitConf.BranchName = this.CheckoutBranch;
                this.CheckoutBranch = null;
                this.CheckoutRemoteBranch = null;

                this.CommitRefresh();
                this.ModifiedRefresh();
                this.LoadCurrentBranche();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Merges the asynchronous.
        /// </summary>
        public void Merge()
        {
            try
            {
                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf.OneConf != null)
                    this.MergeModule(this.GitConf.OneConf);

                if (this.GitConf.TwoConf != null)
                    this.MergeModule(this.GitConf.TwoConf);

                if (this.GitConf.ThirdConf != null)
                    this.MergeModule(this.GitConf.ThirdConf);

                this.CommitRefresh();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Push the asynchronous.
        /// </summary>
        public void Push()
        {
            if (this.BeforePushEvent == null)
            {
                PushGitAsync();
            }
            else
            {
                this.BeforePushEvent?.Invoke();
                if (IsCompiled)
                    this.PushGitAsync();
            }
        }

        /// <summary>
        /// Push the asynchronous.
        /// </summary>
        public void PushGitAsync()
        {
            try
            {
                this.LoadConf();
                this.GitConf = this.GitConfs.FirstOrDefault(m => m.Name == this.RepoName);

                if (this.GitConf.OneConf != null)
                    this.PushModule(this.GitConf.OneConf);

                if (this.GitConf.TwoConf != null)
                    this.PushModule(this.GitConf.TwoConf);

                if (this.GitConf.ThirdConf != null)
                    this.PushModule(this.GitConf.ThirdConf);

                this.CommitRefresh();
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// 刷新待推送的Commit数量
        /// </summary>
        public void CommitRefresh()
        {
            try
            {
                this.CommitCount += this.GitConf?.OneConf?.Repository?.Head?.TrackingDetails?.AheadBy ?? 0;
                this.CommitCount += this.GitConf?.TwoConf?.Repository?.Head?.TrackingDetails?.AheadBy ?? 0;
                this.CommitCount += this.GitConf?.ThirdConf?.Repository?.Head?.TrackingDetails?.AheadBy ?? 0;
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// 文件已修改数量刷新
        /// </summary>
        public void ModifiedRefresh()
        {
            try
            {
                var _changes = new List<YGitStatus>();
                this.ModifiedCount += ModifiedRefresh(this.GitConf?.OneConf, ref _changes);
                this.ModifiedCount += ModifiedRefresh(this.GitConf?.TwoConf, ref _changes);
                this.ModifiedCount += ModifiedRefresh(this.GitConf?.ThirdConf, ref _changes);
                this.Changes = new ObservableCollection<YGitStatus>(_changes);
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// 刷新模块的文件修改数量
        /// </summary>
        /// <param name="conf">YGitRepoConf</param>
        /// <returns>已修改文件数量</returns>
        private int ModifiedRefresh(YGitRepoConf conf, ref List<YGitStatus> trees)
        {
            try
            {
                if (conf == null)
                    return 0;

                this.Initialize(conf);
                // 获取所有修改的文件（包括新添加的文件和删除的文件）

                var changes = conf.Repository.RetrieveStatus();
                foreach (StatusEntry change in changes)
                {
                    switch (change.State)
                    {
                        case FileStatus.NewInIndex:
                        case FileStatus.ModifiedInIndex:
                        case FileStatus.DeletedFromIndex:
                        case FileStatus.RenamedInIndex:
                        case FileStatus.TypeChangeInIndex:
                        case FileStatus.NewInWorkdir:
                        case FileStatus.ModifiedInWorkdir:
                        case FileStatus.DeletedFromWorkdir:
                        case FileStatus.TypeChangeInWorkdir:
                        case FileStatus.RenamedInWorkdir:
                        case FileStatus.Conflicted:
                            trees.Add(new YGitStatus { Repo = this.GitConf.Name, Module = conf.RepoName, Changed = change });
                            break;
                    }
                }

                return trees?.Count ?? 0;
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
                return 0;
            }
        }

        /// <summary>
        /// Pulls the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        private void PullModule(YGitRepoConf conf)
        {
            try
            {
                this.Initialize(conf);

                #region 私仓

                var origin = conf.Repository.Network.Remotes[conf.RemoteName];
                var orefSpecs = origin.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, conf.RemoteName, orefSpecs, fetchOpts, null);
                Commands.Pull(conf.Repository, signature, new PullOptions { FetchOptions = fetchOpts });

                logger.WriteLine($"Repo branch '{conf.RemoteName}/{this.GitConf.BranchName}' pull completed.");

                #endregion

                #region 团仓

                if (string.IsNullOrWhiteSpace(conf.SecondRemoteName) || string.IsNullOrWhiteSpace(conf.SecondRemoteUrl))
                    return;

                var secondOrigin = conf.Repository.Network.Remotes[conf.SecondRemoteName];
                var secondBranch = $"{conf.SecondRemoteName}/{this.GitConf.BranchName}";
                var localBranch = conf.Repository.Branches[this.GitConf.BranchName];
                var secondRefSpecs = secondOrigin.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, conf.SecondRemoteName, secondRefSpecs, fetchOpts, null);
                var mergeResult = conf.Repository.Merge(secondBranch, signature, mergeOpts);

                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    // 提示冲突 
                    logger.WriteLine($"Merge branch '{conf.SecondRemoteName}/{this.GitConf.BranchName}' into {this.GitConf.BranchName} conflict.");
                }
                else
                {
                    // 获取所有修改的文件（包括新添加的文件和删除的文件）
                    var changes = conf.Repository.RetrieveStatus(new StatusOptions() { IncludeIgnored = false, RecurseIgnoredDirs = false, IncludeUnaltered = false });
                    // 获取已修改的文件
                    var modifiedFiles = changes.Modified.Select(c => c.FilePath).ToList();

                    if (modifiedFiles.Any())
                    {
                        foreach (var modifiedFile in modifiedFiles)
                            logger.WriteLine($"{modifiedFile} is modified.");

                        conf.Repository.Commit($"Merge branch '{conf.SecondRemoteName}/{this.GitConf.BranchName}' into {this.GitConf.BranchName} .", signature, signature);
                    }

                    logger.WriteLine($"Repo branch '{conf.SecondRemoteName}/{this.GitConf.BranchName}' pull completed.");
                }

                #endregion

                conf.Branches?.Clear();
                conf.Branches = new ObservableCollection<string>(conf.Repository.Branches.Select(m => m.FriendlyName));
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Clones the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        private void CloneModule(YGitRepoConf conf)
        {
            try
            {
                // Clone the repository
                var path = Repository.Clone(conf.RemoteUrl, conf.LocalPath, cloneOptions);

                if (Directory.Exists(path))
                {
                    this.AddRemote(conf);
                    this.SetPushUrl(conf);
                    this.FetchModule(conf);
                }

                conf.Branches?.Clear();
                conf.Branches = new ObservableCollection<string>(conf.Repository.Branches.Select(m => m.FriendlyName));
                logger.WriteLine($"Repo: {conf.RepoName} , Branche： {this.GitConf.BranchName} clone completed.");
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Commits the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <param name="msg">The MSG.</param>
        private void CommitModule(YGitRepoConf conf, string msg)
        {
            try
            {
                this.Initialize(conf);

                // 获取所有修改的文件（包括新添加的文件和删除的文件）
                var changes = conf.Repository.RetrieveStatus(new StatusOptions() { IncludeIgnored = false, RecurseIgnoredDirs = false, IncludeUnaltered = false });

                // 获取已修改的文件 
                var modifiedFiles = new List<string>();

                foreach (StatusEntry change in changes)
                {
                    switch (change.State)
                    {
                        case FileStatus.NewInIndex:
                        case FileStatus.ModifiedInIndex:
                        case FileStatus.DeletedFromIndex:
                        case FileStatus.RenamedInIndex:
                        case FileStatus.TypeChangeInIndex:
                        case FileStatus.NewInWorkdir:
                        case FileStatus.ModifiedInWorkdir:
                        case FileStatus.DeletedFromWorkdir:
                        case FileStatus.TypeChangeInWorkdir:
                        case FileStatus.RenamedInWorkdir:
                            modifiedFiles.Add(change.FilePath);
                            break;
                        default:
                            break;
                    }
                }

                if (modifiedFiles?.Any() ?? false)
                {
                    Commands.Stage(conf.Repository, modifiedFiles);
                    conf.Repository.Commit(msg, signature, signature);
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Checks the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <exception cref="System.ArgumentNullException">CheckoutBranch</exception>
        private void CheckoutModule(YGitRepoConf conf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.CheckoutBranch))
                    throw new ArgumentNullException(nameof(this.CheckoutBranch));

                if (string.IsNullOrWhiteSpace(this.CheckoutRemoteBranch))
                    throw new ArgumentNullException(nameof(this.CheckoutRemoteBranch));

                this.Initialize(conf);

                var origin = conf.Repository.Network.Remotes[conf.RemoteName];
                var orefSpecs = origin.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, conf.RemoteName, orefSpecs, fetchOpts, null);

                var secondRemote = conf.Repository.Network.Remotes[conf.SecondRemoteName];
                var secondRefSpecs = secondRemote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, conf.SecondRemoteName, secondRefSpecs, fetchOpts, null);

                var localBranch = conf.Repository.Branches[this.CheckoutBranch];
                if (localBranch == null)
                {
                    var branch = conf.Repository.CreateBranch(this.CheckoutBranch, this.checkoutRemoteBranch);
                    Commands.Checkout(conf.Repository, branch, checkoutOpts);
                    conf.Repository.Branches.Update(branch, b => b.Remote = conf.RemoteName, b => b.UpstreamBranch = branch.CanonicalName);
                }
                else
                {
                    Commands.Checkout(conf.Repository, localBranch, checkoutOpts);
                    logger.WriteLine($"{conf.Repository.Head.FriendlyName} checkout completed.");
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Merges the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <exception cref="System.ArgumentNullException">
        /// RemoteName
        /// or
        /// RemoteUrl
        /// </exception>
        private void MergeModule(YGitRepoConf conf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conf.RemoteName))
                    throw new ArgumentNullException(nameof(conf.RemoteName));

                if (string.IsNullOrWhiteSpace(conf.RemoteUrl))
                    throw new ArgumentNullException(nameof(conf.RemoteUrl));

                this.Initialize(conf);
                var origin = conf.Repository.Network.Remotes[conf.RemoteName];
                var originBranch = $"{this.SourceMergeBranch}";
                var orefSpecs = origin.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, conf.RemoteName, orefSpecs, fetchOpts, null);
                var mergeResult = conf.Repository.Merge(originBranch, signature, mergeOpts);

                if (mergeResult.Status == MergeStatus.Conflicts)
                {
                    // 提示冲突 
                    logger.WriteLine($"Merge branch '{conf.SecondRemoteName}/{this.GitConf.BranchName}' into {this.GitConf.BranchName} conflict.");
                }
                else
                {
                    // 获取所有修改的文件（包括新添加的文件和删除的文件）
                    var changes = conf.Repository.RetrieveStatus(new StatusOptions() { IncludeIgnored = false, RecurseIgnoredDirs = false, IncludeUnaltered = false });
                    // 获取已修改的文件
                    var modifiedFiles = changes.Modified.Select(c => c.FilePath).ToList();

                    if (modifiedFiles.Any())
                        conf.Repository.Commit($"Merge branch '{conf.SecondRemoteName}/{this.GitConf.BranchName}' into {this.GitConf.BranchName} .", signature, signature);
                }

                logger.WriteLine($"{conf.RepoName} {originBranch} merge completed.");
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Merges the module.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <exception cref="System.ArgumentNullException">
        /// RemoteName
        /// or
        /// RemoteUrl
        /// </exception>
        private void PushModule(YGitRepoConf conf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conf.RemoteName))
                    throw new ArgumentNullException(nameof(conf.RemoteName));

                if (string.IsNullOrWhiteSpace(conf.RemoteUrl))
                    throw new ArgumentNullException(nameof(conf.RemoteUrl));

                this.Initialize(conf);

                // 获取 origin 远端
                //var remote = conf.Repository.Network.Remotes[conf.RemoteName];
                //conf.Repository.Network.Push(remote, $"refs/heads/{this.GitConf.BranchName}:refs/remotes/{remote.Name}/{this.GitConf.BranchName}", pullOpts);

                var localBranch = conf.Repository.Branches[this.GitConf.BranchName];
                conf.Repository.Network.Push(localBranch, pushOpts);
                logger.WriteLine($"{localBranch.FriendlyName} push completed.");
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }

        }

        /// <summary>
        /// Adds the remote.
        /// </summary>
        /// <param name="conf">The conf.</param>
        private void AddRemote(YGitRepoConf conf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conf.SecondRemoteName))
                    return;

                if (string.IsNullOrWhiteSpace(conf.SecondRemoteUrl))
                    return;

                this.Initialize(conf)?.Network.Remotes.Add(conf.SecondRemoteName, conf.SecondRemoteUrl);
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Sets the push URL.
        /// </summary>
        /// <param name="conf">The conf.</param>
        private void SetPushUrl(YGitRepoConf conf)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(conf.SecondRemoteName))
                    return;

                this.Initialize(conf)?.Network.Remotes.Update(conf.SecondRemoteName, r => r.PushUrl = conf.RemoteUrl);
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Fetches the specified conf.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <exception cref="LibGit2Sharp.NotFoundException">
        /// Remote '{conf.RemoteName}' not found
        /// or
        /// Remote '{conf.TeamRemoteName}' not found
        /// </exception>
        private void FetchModule(YGitRepoConf conf)
        {
            try
            {
                this.Initialize(conf);

                #region origin

                if (string.IsNullOrWhiteSpace(conf.RemoteName))
                    return;

                var remote = conf.Repository.Network.Remotes[conf.RemoteName];

                if (remote == null)
                    throw new NotFoundException($"Remote '{conf.RemoteName}' not found");

                var remoteRefSpecs = remote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, remote.Name, remoteRefSpecs, fetchOpts, null);
                logger.WriteLine($"Fetch repo [{remote.Name}/{this.GitConf.BranchName}] completed.");

                #endregion

                #region secend origin

                if (string.IsNullOrWhiteSpace(conf.SecondRemoteName))
                    return;

                var teamRemote = conf.Repository.Network.Remotes[conf.SecondRemoteName];

                if (teamRemote == null)
                    throw new NotFoundException($"Remote '{conf.SecondRemoteName}' not found");

                var teamRemoteRefSpecs = teamRemote.FetchRefSpecs.Select(x => x.Specification);
                Commands.Fetch(conf.Repository, teamRemote.Name, teamRemoteRefSpecs, fetchOpts, null);
                logger.WriteLine($"Fetch repo [{teamRemote.Name}/{this.GitConf.BranchName}] completed.");
                #endregion

                conf.Branches?.Clear();
                conf.Branches = new ObservableCollection<string>(conf.Repository.Branches.Select(m => m.FriendlyName));
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Checkouts the notify.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="notifyFlags">The notify flags.</param>
        private bool CheckoutNotify(string path, CheckoutNotifyFlags notifyFlags)
        {
            if (notifyFlags.HasFlag(CheckoutNotifyFlags.Conflict))
            {
                logger.WriteLine($"Checkout failed due to conflicts at path: {path}");
                return false;
            }
            else if (notifyFlags.HasFlag(CheckoutNotifyFlags.None))
            {
                logger.WriteLine($"Checkout succeeded at path: {path}");
                return true;
            }
            else if (notifyFlags.HasFlag(CheckoutNotifyFlags.Updated))
            {
                logger.WriteLine($"Checkout updated at path: {path}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Initializes the specified conf.
        /// </summary>
        /// <param name="conf">The conf.</param>
        /// <exception cref="System.ArgumentNullException">LocalPath</exception>
        /// <exception cref="LibGit2Sharp.NotFoundException"></exception>
        private Repository Initialize(YGitRepoConf conf)
        {
            this.LoadConf();

            if (conf == null)
                return default;

            if (string.IsNullOrWhiteSpace(conf.LocalPath))
                throw new ArgumentNullException(nameof(conf.LocalPath));

            if (!Directory.Exists(conf.LocalPath))
                throw new DirectoryNotFoundException($"{conf.LocalPath} not found.");

            if (conf.Repository == null)
                conf.Repository = new Repository(conf.LocalPath);

            return conf.Repository;
        }

        /// <summary>
        /// Called when [git conf changed].
        /// </summary>
        /// <param name="conf">The conf.</param>
        private void OnGitConfChanged(YGitConf conf)
        {
            try
            {
                if (conf != null)
                {
                    signature = new Signature(conf.UserName, conf.Email, DateTimeOffset.Now);
                    pushOpts = new PushOptions { CredentialsProvider = (_url, _user, _cred) => new UsernamePasswordCredentials { Username = conf.UserName, Password = conf.Password }, OnPushStatusError = pushErrorHandler };
                    cloneOptions = new CloneOptions
                    {
                        BranchName = conf.BranchName,
                        OnProgress = progressHandler,
                        OnCheckoutProgress = checkoutProgressHandler,
                        CredentialsProvider = (url, usernameFromUrl, types) =>
                            new UsernamePasswordCredentials
                            {
                                Username = conf.UserName,
                                Password = conf.Password
                            }
                    };

                    this.RepoName = conf.Name;
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Loads the current branches.
        /// </summary> 
        internal void LoadCurrentBranche()
        {
            try
            {
                if (this.GitConf == null)
                {
                    this.CheckoutBranch = null;
                    this.CheckoutRemoteBranch = null;
                    return;
                }

                if (this.Branches?.Any() ?? false)
                    this.CurrentBranch = this.GitConf?.OneConf?.Repository?.Head?.FriendlyName;

                if ((this.RemoteBranchs?.Any() ?? false) && !string.IsNullOrWhiteSpace(this.CurrentBranch))
                    this.CurrentRemoteBranch = this.RemoteBranchs.FirstOrDefault(m => m.Contains(this.CurrentBranch));
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Load Branches
        /// </summary>
        /// <param name="conf">YGitConf</param>
        internal void LoadBranches(YGitConf conf)
        {
            try
            {
                var _branches = new System.Collections.Generic.List<string>();
                var _rbranches = new System.Collections.Generic.List<string>();

                if (conf != null)
                {
                    this.Initialize(conf.OneConf);
                    this.Initialize(conf.TwoConf);
                    this.Initialize(conf.ThirdConf);
                    var _oneBranches = conf.OneConf?.Repository?.Branches.Select(m => m.FriendlyName);
                    var _twoBranches = conf.TwoConf?.Repository?.Branches.Select(m => m.FriendlyName);
                    var _thirdBranches = conf.ThirdConf?.Repository?.Branches.Select(m => m.FriendlyName);

                    if (_oneBranches?.Any() ?? false)
                    {
                        _branches = _oneBranches.Where(m => !m.Contains("/")).ToList();
                        _rbranches = _oneBranches.Where(m => m.Contains("/")).ToList();
                    }

                    if (_twoBranches?.Any() ?? false)
                    {
                        _branches = _branches.Intersect(_twoBranches.Where(m => !m.Contains("/"))).ToList();
                        _rbranches = _rbranches.Intersect(_twoBranches.Where(m => m.Contains("/"))).ToList();
                    }

                    if (_thirdBranches?.Any() ?? false)
                    {
                        _branches = _branches.Intersect(_thirdBranches.Where(m => !m.Contains("/"))).ToList();
                        _rbranches = _rbranches.Intersect(_thirdBranches.Where(m => m.Contains("/"))).ToList();
                    }

                    this.Branches = new ObservableCollection<string>(_branches);
                    this.RemoteBranchs = new ObservableCollection<string>(_rbranches);
                    // this.logger.WriteLine($"Branches is loaded. local branches count: {this.Branches.Count},remote ranches count: {this.RemoteBranchs.Count}");

                    if (this.Branches?.Any() ?? false)
                        this.CurrentBranch = this.GitConf?.OneConf?.Repository?.Head?.FriendlyName;

                    if (this.RemoteBranchs?.Any() ?? false)
                        this.CurrentRemoteBranch = this.RemoteBranchs.FirstOrDefault(m => m.Contains(this.CurrentBranch));
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Loads the conf.
        /// </summary>
        internal void LoadConf()
        {
            try
            {
                if (!(this.GitConfs?.Any() ?? false))
                {
                    var confDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YGit");
                    var confPath = Path.Combine(confDir, "YGits.json");

                    if (File.Exists(confPath))
                    {
                        var json = File.ReadAllText(confPath);
                        this.GitConfs = Newtonsoft.Json.JsonConvert.DeserializeObject<YGitConfs>(json);
                    }
                }

                if (!string.IsNullOrWhiteSpace(this.repoPath))
                {
                    this.GitConf = this.GitConfs.FirstOrDefault(m => m.OneConf.LocalPath == this.repoPath || m.TwoConf?.LocalPath == this.repoPath || m.ThirdConf?.LocalPath == this.repoPath);
                }
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
            }
        }

        /// <summary>
        /// Saves the conf.
        /// </summary>
        private void SaveConf()
        {
            var confDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YGit");
            var confPath = Path.Combine(confDir, "YGit.json");

            if (File.Exists(confPath))
                File.Delete(confPath);

            if (!Directory.Exists(confDir))
                Directory.CreateDirectory(confDir);

            if (this.GitConf.OneConf != null)
                this.GitConf.OneConf.LocalPath = Path.Combine(this.GitConf.RootPath, this.GitConf.OneConf.RepoName);

            if (this.GitConf.TwoConf != null)
                this.GitConf.TwoConf.LocalPath = Path.Combine(this.GitConf.RootPath, this.GitConf.TwoConf.RepoName);

            if (this.GitConf.ThirdConf != null)
                this.GitConf.ThirdConf.LocalPath = Path.Combine(this.GitConf.RootPath, this.GitConf.ThirdConf.RepoName);

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this.GitConf);
            File.WriteAllText(confPath, json);
        }

        /// <summary>
        /// Saves the conf.
        /// </summary>
        private void SaveConfs()
        {
            var confDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "YGit");
            var confPath = Path.Combine(confDir, "YGitS.json");

            if (File.Exists(confPath))
                File.Delete(confPath);

            if (!Directory.Exists(confDir))
                Directory.CreateDirectory(confDir);

            if (this.GitConfs?.Any() ?? false)
            {
                foreach (var conf in this.GitConfs)
                {
                    if (conf.OneConf != null)
                        conf.OneConf.LocalPath = Path.Combine(conf.RootPath, conf.OneConf.RepoName);

                    if (conf.TwoConf != null)
                        conf.TwoConf.LocalPath = Path.Combine(conf.RootPath, conf.TwoConf.RepoName);

                    if (conf.ThirdConf != null)
                        conf.ThirdConf.LocalPath = Path.Combine(conf.RootPath, conf.ThirdConf.RepoName);
                }
            }

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this.GitConfs);
            File.WriteAllText(confPath, json);
        }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <param name="directoryInfo">The directory information.</param>
        /// <returns>
        ///   <c>true</c> if the specified directory information is valid; otherwise, <c>false</c>.
        /// </returns>
        private bool IsValid(DirectoryInfo directoryInfo)
        {
            try
            {
                var valied = Repository.IsValid(directoryInfo.FullName);
                if (valied)
                {
                    this.repoPath = directoryInfo.FullName;
                    return valied;
                }

                if (directoryInfo.Parent != null)
                    return this.IsValid(directoryInfo.Parent);

                return false;
            }
            catch (Exception ex)
            {
                this.logger.WriteLine($"Error: {MethodBase.GetCurrentMethod().Name} Fail.\n-----{ex}");
                return false;
            }
        }
    }
}
