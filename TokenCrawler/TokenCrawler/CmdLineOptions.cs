using Plossum.CommandLine;

namespace TokenCrawler
{
    /// <summary>
    /// This class is used by Plossum Command Line package in order to manage the Command Line options
    /// </summary>
    [CommandLineManager(ApplicationName = "\nToken Crawler",
        Copyright = "This utility will crawl the websites refered in txt file and searches for a specific token in the HMTL or in the js files used by the site")]
    class CmdLineOptions
    {
        [CommandLineOption(Description = "Displays this help text")]
        public bool Help = false;

        [CommandLineOption(Description = "Specifies the input file that contains the links to crawl (one per line)", MinOccurs = 0)]
        public string File
        {
            get { return mFile; }
            set
            {
                mFile = value;
            }
        }

        [CommandLineOption(Description = "Specifies the token (can be defined as a regular expression) to be searched for in the crawled files", MinOccurs = 1)]
        public string Token
        {
            get { return mToken; }
            set
            {
                mToken = value;
            }
        }

        [CommandLineOption(Description = "Identify verbose level:0 minimum, 1 normal, 2 full")]
        public int Verbose
        {
            get { return mVerbose; }
            set
            {
                mVerbose = value;
            }
        }

        [CommandLineOption(Description = "The maximum number of result excerts to show when the pattern is found. 0 means unlimited. ")]
        public int MaxResults
        {
            get { return mMaxResults; }
            set
            {
                mMaxResults = value;
            }
        }

        [CommandLineOption(Description = "The file to output the results of execution to")]
        public string Output
        {
            get { return mOutput; }
            set
            {
                mOutput = value;
            }
        }

        private string mFile="sites.txt";
        private string mToken="hitslink";
        private string mOutput = "";
        private int mMaxResults = 5;
        private int mVerbose=1;
    }

}
