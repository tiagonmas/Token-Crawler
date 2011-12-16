using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plossum.CommandLine;

namespace TokenCrawler
{

    [CommandLineManager(ApplicationName = "\nHitLinks Site Crawler",
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

        [CommandLineOption(Description = "Specifies the token to be searched for in the crawled files", MinOccurs = 0)]
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

        private string mFile="sites.txt";
        private string mToken="hitslink";
        private int mVerbose=1;
    }

}
