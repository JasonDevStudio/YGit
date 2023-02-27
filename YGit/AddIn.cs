using Autofac;
using YGit.Common;

namespace YGit
{
    internal class AddIn : YGit.Common.AddIn
    {
        public override void OnRegsisterTypes(ContainerBuilder container)
        {
            GlobaService.ContainerBuilder.RegisterType<Logger>().As<ILogger>().SingleInstance().PropertiesAutowired(); 
        } 
    }
}
