using System;
using Plossum.CommandLine;
using System.Linq;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace TokenCrawler
{
    class Program
    {
        static CmdLineOptions cmdLine;
        //static int VerboseLevel;
        static Regex regexToken;
        static Dictionary<string, string> findings; //will contain sites that satisfy criteria, and an excert
        static Output output; //will write to file findings


        static int Main(string[] args)
        {
            int crawlnum = 0; //number of sites crawled
            int crawltotal; //number of sites to crawl.

            #region CommandLineArgsProcessing
            cmdLine = new CmdLineOptions();
            CommandLineParser parser = new CommandLineParser(cmdLine);
            parser.Parse();
            Console.WriteLine(parser.UsageInfo.GetHeaderAsString(78));

            if (cmdLine.Help)
            {
                Console.WriteLine(parser.UsageInfo.GetOptionsAsString(78));
                return 0;
            }
            else if (parser.HasErrors)
            {
                Console.WriteLine(parser.UsageInfo.GetErrorsAsString(78));
                return -1;
            }

            

            //Create output file
            if (String.IsNullOrEmpty(cmdLine.Output))
            { output = new Output(); }
            else
            { output = new Output(cmdLine.Output); }

            try
            {
                if (cmdLine.IgnoreCase)
                { regexToken = new Regex(cmdLine.Token, RegexOptions.IgnoreCase); }
                else
                {regexToken = new Regex(cmdLine.Token);}
            }
            catch (System.Exception exc)
            {
                Console.WriteLine("Error parsing Token Regular Expression: " + cmdLine.Token + ".\nDetails:" + exc.Message);
                return -1;
            }
            
            #region ShowInitialParametersinConsoleAndFile
            StringBuilder str = new StringBuilder();
            
            str.Append("Running with the following commands:\n");
            str.Append(String.Format("\tRegex Token: {0}\n", cmdLine.Token));
            str.Append(String.Format("\tIgnoreCase: {0}\n", cmdLine.IgnoreCase));
            str.Append(String.Format("\tFile: {0}\n", cmdLine.File));
            str.Append(String.Format("\tVerbose Level: {0}\n", cmdLine.Verbose));
            str.Append(String.Format("\tOutput Results to: {0}\n", output.FileName));
            str.Append(String.Format("\tMaxResults: {0}\n", cmdLine.MaxResults));

            output.WriteLine(str.ToString());
            Console.WriteLine(str.ToString());
            str = null;
            #endregion
            #endregion

            #region ReadSitesFromFileAndCrawl

            try
            {
                
                findings= new Dictionary<string, string>();

                string[] sites = GetSitesFromFile(cmdLine.File);
                sites = RemoveCommentsAndPrepURL(sites); //this could be improved. 
                crawltotal=sites.Count();
                Console.WriteLine(String.Format("Started to crawl {0} sites\n",crawltotal));
                foreach (string siteUrl in sites)
                {
                    crawlnum++; //increase crawl counter.

                    Inform(String.Format("\nCrawling ({0}/{1}) {2} \n", crawlnum, crawltotal, siteUrl), 1);

                    //show something is happening if we are in verbose=0 mode
                    if (cmdLine.Verbose == 0) Console.Write(".");

                    try
                    {
                        string html, excert;
                        if (!DownloadAndFindToken(siteUrl, cmdLine.Token, out html, out excert))
                        { //it was not found on the main HTML page, check all referred scripts

                            #region CheckInternalScripts
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            var head = doc.DocumentNode.Descendants().Where(n => n.Name == "head").FirstOrDefault();
                            foreach (var script in doc.DocumentNode.Descendants("script").ToArray())
                            {
                                string script_url = String.Empty;

                                //show something is happening if we are in verbose=0 mode
                                if (cmdLine.Verbose < 2) Console.Write(".");

                                //Console.Write(script.Value);
                                HtmlAttribute att = script.Attributes["src"];
                                if (att != null && !String.IsNullOrEmpty(att.Value))
                                {
                                    //check the absolute path to the script
                                    string initialchars = att.Value.Substring(0, 5).ToLower();
                                    if (initialchars.StartsWith("http") || initialchars.StartsWith("https")) script_url = att.Value;
                                    else if (att.Value.StartsWith("//")) script_url = "http:" + att.Value;
                                    else if (att.Value.StartsWith("/")) script_url = siteUrl + att.Value;
                                    else script_url = siteUrl + "/" + att.Value; //TO DO: take care of relative urls

                                    Inform(String.Format("\t{0}\n", script_url), 2);
                                    try
                                    {
                                        if (DownloadAndFindToken(script_url, cmdLine.Token, out html, out excert))
                                        {
                                            RecordFoundSite(script_url, excert);
                                            break;
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Inform(String.Format("\tError loading {0}\n", script_url), 1);
                                        Console.ResetColor();
                                    }
                                }

                            }
                            doc = null; //release doc

                            #endregion

                        }
                        else { RecordFoundSite(siteUrl, excert); }

                    }
                    catch (System.Net.WebException exc)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Error. Unable to open site {0}", exc.Message);
                        Console.ResetColor();
                    }
                    
                }

                #region DumpFoundFiles
                ///Dump found files
                Console.ForegroundColor = ConsoleColor.Yellow;
                if (findings.Count == 0)
                { Console.WriteLine("\n\n{0} was not found on any of the sites searched:\n", cmdLine.Token); Console.ResetColor();}
                else
                { 
                    Console.WriteLine("\n\n\"{0}\" was found on the following {1} sites:\n", cmdLine.Token, findings.Count); 
                    Console.ResetColor();
                    foreach (string foundit in findings.Keys)
                    {
                        Console.WriteLine("\t"+foundit);
                    }

                }
                #endregion
            }
            catch (System.IO.FileNotFoundException exc)
            {
                Console.WriteLine("Error. Unable to open file {0}.\n",  exc.Message);
                
            }

            catch (Exception exc)
            {
                Console.WriteLine("Error: {0}\n",exc.ToString());
            }

            #endregion

            return 0;
        }




        #region HelpFunctions
        /// <summary>
        /// Remove comments from list of sites, and prepare urls.
        /// </summary>
        /// <param name="sites"></param>
        /// <returns></returns>
        private static string[] RemoveCommentsAndPrepURL(string[] sites)
        {   
            List<string> newsites=new List<string>(sites.Count());
            foreach (string site in sites)
            {
                string str=site.Trim();
                if(!str.StartsWith("///")) //if it is not a comment
                {newsites.Add(PrepURL(site));}

            }
            return newsites.ToArray<string>();
        }
        private static void RecordFoundSite(string url, string excert)
        {
            try
            {
                findings.Add(url, excert);
            }
            catch(System.ArgumentException){}; //do nothing if it was already in the dictionary
            output.WriteLine(String.Format("{0}\t{1}", url, excert));
        }
        /// <summary>
        /// Download the html of a given url and check if it contains a token
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="outHTML"></param>
        /// <returns></returns>
        private static bool DownloadAndFindToken(string url,string token, out string outHTML, out string excert)
        {
            excert = null;
            WebClient client = new WebClient();
            //some urls have spaces and browsers support. Need to take spaces out of urls
            url=url.Replace(" ", "");
            outHTML = client.DownloadString(url);
            bool found = regexToken.IsMatch(outHTML);
            if (found)
            {
                //Show what was found

                StringBuilder sb = new StringBuilder();
                MatchCollection matches = regexToken.Matches(outHTML);
                
                for (int i = 0; i < matches.Count; i++)
                {
                    string substr = SubStringInform(outHTML, matches[i].Index);
                    //substr=Regex.Replace(substr, @"\s", "");
                    substr = substr.Replace("\n", "").Replace("  ", "").Replace("\t", "").Replace("\r", "");
                    
                    //is the match something "simple" to show to the user?
                    if (cmdLine.Token.Contains(matches[i].ToString()))
                    { sb.Append("\t" + i + ": " + matches[i] + "\t " + substr + "\n"); }
                    else
                    { sb.Append("\t" + i + ": " + cmdLine.Token + "\t " + substr + "\n"); }
                    //did we reach max results to show ?
                    
                    if (cmdLine.MaxResults!=0 && cmdLine.MaxResults <= i+1) break;
                }
                excert = sb.ToString();

                Console.ForegroundColor = ConsoleColor.Green;
                if (cmdLine.MaxResults > matches.Count)
                { Inform("Token Found in " + matches.Count + " places:" + "\n"+excert, 1); }
                else
                { Inform("Token Found in " + matches.Count + " places. Showing only " + cmdLine.MaxResults + "\n" + excert, 1); }
                Console.ResetColor();
                Console.Write("\n\n");
            }
            client = null; //release client
            
            return found;
        }

        /// <summary>
        /// Get the substring of the original string that contains the token, in order to show where the token was found
        /// </summary>
        /// <param name="html">the original html</param>
        /// <param name="token">the token that was found</param>
        /// <param name="found">where the token was found</param>
        /// <returns></returns>
        private static string SubStringInform(string html, int found)
        {   const int subsize=50;  //desired size of substring to show
            
            //start of text
            if (found==0) 
                if (html.Length>subsize)
                    return html.Substring(0,subsize);
                else return html.Substring(found, html.Length);
            //end of text
            else if (found + subsize >= html.Length)
                return html.Substring(found, html.Length-found);
            //middle of string
            else if (found - subsize/2 < 0) 
                return html.Substring(0, subsize);

            return html.Substring(found - subsize / 2, subsize);

        }

        /// <summary>
        /// Opens a File, reads all lines and returns the contents split by \r and \n
        /// </summary>
        /// <param name="response">The file to read</param>
		private static string[] GetSitesFromFile(string file) {
            char[] separator = new char[] { '\n', '\r' };
            return new StreamReader(File.OpenRead(file)).ReadToEnd().Split(separator, StringSplitOptions.RemoveEmptyEntries);

		}


        /// <summary>
        /// Make sure a URL starts with http 
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <returns></returns>
        private static string PrepURL(string siteUrl)
        {

            if (siteUrl.Contains("http://") || siteUrl.Contains("https://"))
                return siteUrl;
            else
                return "http://" + siteUrl;
        }

        /// <summary>
        /// Write to the console if level is >= then specified in command line
        /// </summary>
        /// <param name="str">The string to Output to console</param>
        /// <param name="level">The threshold level to output or not</param>
        private static void Inform(string str, int level)
        {
            if (level <= cmdLine.Verbose) Console.Write(str);
        }
        #endregion
    }
}
