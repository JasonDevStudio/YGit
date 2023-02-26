// See https://aka.ms/new-console-template for more information

using LibGit2Sharp.Handlers;
using LibGit2Sharp;
using YGit.Model;
using YGit.ViewModel;
using YGit.Common;

var addin = new YGitConsole.AddIn();
addin.RegsisterTypes();

var gitConf = new YGitConf()
{
    BranchName ="develop",
    Email = "yaojiestudio@gmail.com",
    Password = "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",
    RootPath = "D:\\Git",
    UserName = "y95535",
    OneConf = new YGitRepoConf
    {
        RemoteName = "origin",
        RemoteUrl = "https://github.com/y95535/YGit.git",
        RepoName = "YGit",
        SecondRemoteName = "team_origin",
        SecondRemoteUrl = "https://github.com/JasonDevStudio/YGit.git"
    }
};

var vm = new YGitVM();

#region Clone 仓库
await vm.CloneAsync(); // Clone 仓库
#endregion

#region 提交修改文件
//vm.CModule = "Test";
//vm.CMsg = "提交测试";
//vm.CommitCmd.Execute(null);
#endregion

#region Push 推送
//await vm.PullAsync() // 推送
#endregion

#region Checkout 签出分支 
vm.CheckoutBranch = "main";
vm.CheckoutRemoteBranch = "origin/main";
await vm.CheckoutAsync();
#endregion

#region Pull 拉取远端分支到本地分支
//await vm.PullAsync();
#endregion

#region Merge 合并分支

vm.SourceMergeBranch = "origin/develop";
await vm.MergeAsync();

#endregion

Console.ReadLine();
