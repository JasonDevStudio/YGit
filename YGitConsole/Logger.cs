using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YGit;

namespace YGitConsole
{
    internal class Logger : ILogger
    {
        public bool WriteLine(string message)
        {
            Console.WriteLine(message);
            return true;
        }
    }
}
