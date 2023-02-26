using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YGit.Common;
using YGit;
using Autofac;

namespace YGitConsole
{
    internal class AddIn : YGit.Common.AddIn
    {
        public override void OnRegsisterTypes(ContainerBuilder container)
        {
            GlobaService.ContainerBuilder.RegisterType<Logger>().As<ILogger>().PropertiesAutowired(); 
        } 
    }
}
