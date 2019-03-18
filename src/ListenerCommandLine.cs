#region header
// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Robert Vandehey" file="ListenerCommandLine.cs">
// MIT License
// 
// Copyright(c) 2018 Robert Vandehey
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#endregion
using System;
using System.Diagnostics;
using System.IO;

namespace SynchroFeed.Listener
{
    /// <summary>
    /// The ListenerCommandLine class is contains the command line options.
    /// </summary>
    public class ListenerCommandLine
    {
        private const string DefaultConfigFile = "app.json";

        private string configFile;

        /// <summary>
        /// Gets the name of the configuration file.
        /// </summary>
        /// <value>The name of the configuration file.</value>
        public string ConfigFile {
            get { return string.IsNullOrEmpty(configFile) ? DefaultConfigFile : configFile; }
            set { configFile = value; }
        }

        public string GetFullPathConfigFile()
        {
            var filename = ConfigFile;
            var rootPath = Path.GetDirectoryName(filename);
            if (string.IsNullOrEmpty(rootPath))
            {
                filename = Path.Combine(GetBaseExePath(), filename);
            }
            return Path.GetFullPath(filename);
        }

        private string GetBaseExePath()
        {
            var path = Process.GetCurrentProcess().MainModule.FileName;
            if (!string.IsNullOrEmpty(path))
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }
    }
}
