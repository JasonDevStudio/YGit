using Autofac;

namespace YGit.Common
{
    internal abstract class AddIn
    {
        public void RegsisterTypes()
        {
            OnRegsisterTypes(GlobaService.ContainerBuilder);
            GlobaService.Container = GlobaService.ContainerBuilder.Build();
        }

        public abstract void OnRegsisterTypes(ContainerBuilder container);
    }
}
