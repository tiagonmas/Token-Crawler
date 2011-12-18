using System;
using System.IO;

namespace TokenCrawler
{
    class Output
    {
        /// <summary>
        /// This class takes care of writing to a file the results of the execution of the program
        /// </summary>
        TextWriter tw;
        private string _fileName;
        public string FileName { get {return _fileName;} }

        public Output()
        {
            // create a writer and open the file
            _fileName = "TokenCrawler" + DateTime.Now.ToShortDateString().Replace("-", "_") + "_" + DateTime.Now.ToShortTimeString().Replace(":", "_") + ".txt";
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
            // close the stream
            try{tw.Close();}
                catch{};
        }
    }
}
