using Plossum.CommandLine;

namespace TokenCrawler
{
    /// <summary>
    /// This class is used by Plossum Command Line package in order to manage the Command Line options
    /// </summary>
    [CommandLineManager(ApplicationName = "\nToken Crawler",
        Copyright = "This utility crawls websites and searches for specific patterns in the HMTL and the js files used by the site(s)",
        EnabledOptionStyles = OptionStyles.Group | OptionStyles.Windows)]
    [CommandLineOptionGroup("input", Name = "Input (chose one)",
       Require = OptionGroupRequirement.ExactlyOne)]
    [CommandLineOptionGroup("options", Name = "Other options")]
    class CmdLineOptions
    {
        [CommandLineOption(Description = "Displays this help text", GroupId = "options")]
        public bool Help = false;

        [CommandLineOption(Description = "Specifies the input file that contains the links to crawl (one per line)"
            ,GroupId = "input", MinOccurs=0)]
        public string File
        {
            get { return mFile; }
            set
            {
                mFile = value;
            }
        }

        [CommandLineOption(Description = "Specifies the url to be crawled looking for the pattern. if crawling multiple sites, use -File option.", 
            GroupId="input")]
        public string Url
        {
            get { return mUrl; }
            set
            {
                mUrl = value;
            }
        }

        [CommandLineOption(Description = "Specifies the token (can be defined as a regular expression) to be searched for in the crawled files",
             MinOccurs = 1)]
        public string Token
        {
            get { return mToken; }
            set
            {
                mToken = value;
            }
        }

        [CommandLineOption(Description = "Specifies the token (can be defined as a regular expression) to be searched for in the crawled files (default is true)", 
            GroupId = "options",MinOccurs = 0)]
        public bool IgnoreCase
        {
            get { return mIgnoreCase; }
            set
            {
                mIgnoreCase = value;
            }
        }

        [CommandLineOption(Description = "find the pattern on the Response Headers of the HTTP request ? defaults to false",
            GroupId = "options")]
        public bool Headers
        {
            get { return mHeaders; }
            set
            {
                mHeaders = value;
            }
        }

        [CommandLineOption(Description = "Identify verbose level:0 minimum, 1 normal, 2 full (defaults to 1)"
             ,GroupId = "options", MaxValue=2, MinValue=0)]
        public int Verbose
        {
            get { return mVerbose; }
            set
            {
                mVerbose = value;
            }
        }

        [CommandLineOption(Description = "The maximum number of result excerts to show when the pattern is found. 0 means unlimited.(defaults to 5) "
            , GroupId = "options")]
        public int MaxResults
        {
            get { return mMaxResults; }
            set
            {
                mMaxResults = value;
            }
        }

        [CommandLineOption(Description = "The file to output the results of execution to", GroupId = "options")]
        public string Output
        {
            get { return mOutput; }
            set
            {
                mOutput = value;
            }
        }

        private string mFile;
        private string mUrl;
        private string mToken="";
        private string mOutput = "";
        private int mMaxResults = 5;
        private int mVerbose=1;
        private bool mIgnoreCase = true;
        private bool mHeaders = false;
    }

}
