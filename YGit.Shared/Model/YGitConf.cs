using System.Collections.Generic;
using System.Collections.ObjectModel; 
using CommunityToolkit.Mvvm.ComponentModel;
using LibGit2Sharp;
using Newtonsoft.Json; 

namespace YGit.Model
{
    /// <summary>
    /// YGitConfs
    /// </summary>
    internal class YGitConfs: ObservableCollection<YGitConf>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="YGitConfs"/> class.
        /// </summary>
        public YGitConfs() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="YGitConfs"/> class.
        /// </summary>
        /// <param name="confs">The confs.</param>
        public YGitConfs(IEnumerable<YGitConf> confs)
        {
            foreach (var item in confs) 
                this.Add(item); 
        }
    }

    /// <summary>
    /// Represents a model of the YGit configuration.
    /// </summary> 
    internal class YGitConf : ObservableObject
    {
        private string userName;
        private string password;
        private string email;
        private string rootPath;
        private string branchName;
        private YGitRepoConf oneConf;
        private YGitRepoConf twoConf;
        private YGitRepoConf thirdConf;

        /// <summary>
        /// Gets or sets the name of the user.
        /// </summary>
        /// <value>
        /// The name of the user.
        /// </value>
        public string UserName { get => userName; set => this.SetProperty(ref this.userName, value); }

        /// <summary>
        /// Gets or sets the email.
        /// </summary>
        /// <value>
        /// The email.
        /// </value>
        public string Email { get => this.email; set => this.SetProperty(ref this.email, value); }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>
        /// The password.
        /// </value>
        public string Password { get => password; set => this.SetProperty(ref this.password, value); }

        /// <summary>
        /// Gets or sets the root path.
        /// </summary>
        /// <value>
        /// The root path.
        /// </value>
        public string RootPath { get => rootPath; set => this.SetProperty(ref this.rootPath, value); }

        /// <summary>
        /// Gets or sets the name of the branch.
        /// </summary>
        /// <value>
        /// The name of the branch.
        /// </value>
        public string BranchName { get => branchName; set => this.SetProperty(ref this.branchName, value); }

        /// <summary>
        /// Gets or sets the one conf.
        /// </summary>
        /// <value>
        /// The one conf.
        /// </value>
        public YGitRepoConf OneConf { get => oneConf; set => this.SetProperty(ref this.oneConf, value); }

        /// <summary>
        /// Gets or sets the two conf.
        /// </summary>
        /// <value>
        /// The two conf.
        /// </value>
        public YGitRepoConf TwoConf { get => twoConf; set => this.SetProperty(ref this.twoConf, value); }

        /// <summary>
        /// Gets or sets the third conf.
        /// </summary>
        /// <value>
        /// The third conf.
        /// </value>
        public YGitRepoConf ThirdConf { get => thirdConf; set => this.SetProperty(ref this.thirdConf, value); }
    }
     
    /// <summary>
    /// This class provides methods to get the configuration information of a git repository.
    /// </summary>
    internal class YGitRepoConf : ObservableObject
    { 
        private string repoName;
        private string remoteName; 
        private string remoteUrl;        
        private string localPath;
        private string teamRemoteName;
        private string teamRemoteUrl;
        private ObservableCollection<string> branches;

        [JsonIgnore]
        public Repository Repository { get; set; }
        public string RepoName { get => repoName; set => this.SetProperty(ref this.repoName, value); }
        public string RemoteName { get => remoteName; set => this.SetProperty(ref this.remoteName, value); }
        public string RemoteUrl { get => remoteUrl; set => this.SetProperty(ref this.remoteUrl, value); }      
        public string LocalPath { get => localPath; set => this.SetProperty(ref this.localPath, value); }
        public string SecondRemoteName { get => teamRemoteName; set => this.SetProperty(ref this.teamRemoteName, value); }
        public string SecondRemoteUrl { get => teamRemoteUrl; set => this.SetProperty(ref this.teamRemoteUrl, value); }
        public ObservableCollection<string> Branches { get => branches; set => this.SetProperty(ref this.branches, value); }
    }
}
