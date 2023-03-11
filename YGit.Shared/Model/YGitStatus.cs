using System;
using System.Collections.Generic;
using System.Text;
using LibGit2Sharp;

namespace YGit.Model
{
    internal class YGitStatus
    { 
        public string Repo { get; set; }

        public string Module { get; set; }

        public string Status
        {
            get
            {
                switch (Changed.State)
                {
                    case FileStatus.NewInWorkdir:
                    case FileStatus.NewInIndex:
                        return "Added";
                    case FileStatus.ModifiedInWorkdir:
                    case FileStatus.ModifiedInIndex:
                        return "Modified";
                    case FileStatus.DeletedFromWorkdir:
                    case FileStatus.DeletedFromIndex:
                        return "Deleted";
                    case FileStatus.RenamedInWorkdir:
                    case FileStatus.RenamedInIndex:
                        return "Renamed";
                    case FileStatus.TypeChangeInWorkdir:
                    case FileStatus.TypeChangeInIndex:
                        return "TypeChanged";
                    case FileStatus.Ignored:
                        return nameof(FileStatus.Ignored); 
                    default:
                        return "None";
                }
            }
        }

        public StatusEntry Changed { get; set; }
    }
}
