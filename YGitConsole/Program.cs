// See https://aka.ms/new-console-template for more information

using LibGit2Sharp.Handlers;
using LibGit2Sharp;
using YGit.Model;
using YGit.ViewModel;
using YGit.Common;

var addin = new YGitConsole.AddIn();
addin.RegsisterTypes();

var confs = new YGitConfs();
var gitConf = new YGitConf()
{
    Name = "OpenCharts_Develop",
    BranchName = "develop",
    Email = "yaojiestudio@outlook.com",
    Password = "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",
    RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "OpenCharts"),
    UserName = "y95536",
    OneConf = new YGitRepoConf
    {
        RemoteName = "origin",
        RemoteUrl = "https://github.com/y95536/OpenCharts.git",
        RepoName = "framework",
        SecondRemoteName = "team_origin",
        SecondRemoteUrl = "https://github.com/y95535/OpenCharts.git"
    },
    TwoConf = new YGitRepoConf
    {
        RemoteName = "origin",
        RemoteUrl = "https://github.com/y95536/OpenCharts.git",
        RepoName = "modules",
        SecondRemoteName = "team_origin",
        SecondRemoteUrl = "https://github.com/y95535/OpenCharts.git"
    }
};

confs.Add(gitConf);
var vm = new YGitVM(); //"D:\\Git\\YGit\\YGit.Shared" 

#region Save Conf

vm.GitConfs = confs;
vm.SaveConfsCmd.Execute(null);

#endregion

#region Load Conf
//vm.RepoPath = "D:\\Git\\YGit\\YGitConsole";
#endregion

#region Clone 仓库
//vm.RepoName = "YGit_Develop";
//await vm.CloneAsync(); // Clone 仓库
#endregion

#region 提交修改文件
//vm.CModule = "Readme";
//vm.CMsg = "2023-2-26 16:19:21";
//vm.CommitCmd.Execute(null);
#endregion

#region Push 推送
//await vm.PushAsync(); // 推送
#endregion

#region Checkout 签出分支 
//vm.CheckoutBranch = "main";
//vm.CheckoutRemoteBranch = "origin/main";
//await vm.CheckoutAsync();
#endregion

#region Pull 拉取远端分支到本地分支
//await vm.PullAsync();
#endregion

#region Merge 合并分支

//vm.SourceMergeBranch = "origin/develop";
//await vm.MergeAsync();

#endregion

Console.ReadLine();
