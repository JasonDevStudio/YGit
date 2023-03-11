using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace YGit
{
    public class Logger: ILogger
    {
        private readonly IVsOutputWindowPane outputPane;

        public Logger(IVsOutputWindow outputWindow, string name)
        {
            Guid guid = Guid.NewGuid();
            outputWindow.CreatePane(ref guid, name, 1, 1);
            outputWindow.GetPane(ref guid, out outputPane);
        }

        public Logger():this(YGitPackage.vsOutput, YGitPackage.LoggerName)
        { 
        }

        public bool WriteLine(string message)
        {
            outputPane.Activate();
            outputPane.OutputStringThreadSafe(message + Environment.NewLine); 
            return true;
        } 
    }
}
