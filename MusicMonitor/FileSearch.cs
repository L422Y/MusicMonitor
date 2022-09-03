using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MusicMonitor
{
    public class FileSearch
    {
        ArrayList _extensions;
        bool _recursive;
        public ArrayList SearchExtensions
        {
            get { return _extensions; }
        }
        public bool Recursive
        {
            get { return _recursive; }
            set { _recursive = value; }
        }
        public FileSearch()
        {
            _extensions = ArrayList.Synchronized(new ArrayList());
            _recursive = true;
        }
        public FileInfo[] Search(string path)
        {
            DirectoryInfo root = new DirectoryInfo(path);
            ArrayList subFiles = new ArrayList();
            foreach (FileInfo file in root.GetFiles())
            {
                if (_extensions.Contains(file.Extension))
                {
                    subFiles.Add(file);
                }
            }
            if (_recursive)
            {
                foreach (DirectoryInfo directory in root.GetDirectories())
                {
                    subFiles.AddRange(Search(directory.FullName));
                }
            }
            return (FileInfo[])subFiles.ToArray(typeof(FileInfo));
        }
    }
}
