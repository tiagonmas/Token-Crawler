using System;
using System.IO;

namespace TokenCrawler
{
    class Output : IDisposable
    {
        /// <summary>
        /// This class takes care of writing to a file the results of the execution of the program
        /// </summary>
        TextWriter tw;
        private string _fileName;
        private bool disposed;
        public string FileName { get {return _fileName;} }

        public Output()
        {
            // create a writer and open the file
           _fileName = string.Format("Log_{0}.txt",DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
           tw = new StreamWriter(_fileName);

        }

        public Output(string FileName)
        {
            // create a writer and open the file
            tw = new StreamWriter(FileName);
            _fileName = FileName;
        }

        public void Write(string str)
        {
            // write a line of text to the file
            tw.Write(str);
            tw.Flush();
        }

        public void WriteLine(string str)
        {
            // write a line of text to the file
            tw.WriteLine(str);
            tw.Flush();
        }

        ~Output()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Close the stream
                tw.Close();
            }
            disposed = true;
        }
    }
}
