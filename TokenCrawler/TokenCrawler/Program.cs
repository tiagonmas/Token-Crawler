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
        const string Tab = "\t";

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
                Console.WriteLine("Error parsing Token Regular Expression: {0}.", cmdLine.Token);
                Console.WriteLine("Details:", exc.Message);
                return -1;
            }
            
            #region ShowInitialParametersinConsoleAndFile
            StringBuilder str = new StringBuilder();
            
            str.AppendLine("Running with the following commands:");
            str.AppendFormat("{0}Regex Token:{0}{1}{2}", Tab, cmdLine.Token, Environment.NewLine);
            if (cmdLine.Url != null) //are we searching for one url or for all urls in a file ?
            { str.AppendFormat("{0}Url:{0}{1}{2}", Tab, cmdLine.Url, Environment.NewLine); }
            else
            { str.AppendFormat("{0}File:{0}{1}{2}", Tab, cmdLine.File, Environment.NewLine); }

            str.AppendFormat("{0}Find in HTTP Headers:{0}{1}{2}", Tab, cmdLine.Headers, Environment.NewLine);
            str.AppendFormat("{0}IgnoreCase:{0}{1}{2}", Tab, cmdLine.IgnoreCase, Environment.NewLine);
            str.AppendFormat("{0}Verbose Level:{0}{1}{2}", Tab, cmdLine.Verbose, Environment.NewLine);
            str.AppendFormat("{0}Output Results to:{0}{1}{2}", Tab, output.FileName, Environment.NewLine);
            str.AppendFormat("{0}MaxResults:{0}{1}{2}", Tab, cmdLine.MaxResults, Environment.NewLine);
            str.AppendFormat("{0}User Agent:{0}{1}{2}", Tab, cmdLine.UserAgent, Environment.NewLine);

            output.WriteLine(str.ToString());
            Console.WriteLine(str.ToString());

            #endregion
            #endregion

            #region ReadSitesFromFileAndCrawl

            try
            {
                string[] sites; //sites to be crawled

                findings= new Dictionary<string, string>();
                if (cmdLine.Url != null) //are we searching for one url or for all urls in a file ?
                { 
                    sites = new string[] { PrepURL(cmdLine.Url.ToString()) }; 
                    crawltotal = 1;
                    Console.WriteLine("Crawling {0}{1}", cmdLine.Url, Environment.NewLine);
                }
                else
                {
                    sites = GetSitesFromFile(cmdLine.File);
                    sites = RemoveCommentsAndPrepURL(sites); //this could be improved. 
                    crawltotal = sites.Count();
                    Console.WriteLine("Started to crawl {0} sites{1}", crawltotal, Environment.NewLine);
                }
                
                foreach (string siteUrl in sites)
                {
                    crawlnum++; //increase crawl counter.

                    Inform(String.Format("{3}Crawling ({0}/{1}) {2} {3}", crawlnum, crawltotal, siteUrl, Environment.NewLine), 1);

                    //show something is happening if we are in verbose=0 mode
                    if (cmdLine.Verbose == 0) Console.Write(".");

                    try
                    {
                        string html;
                        if (!DownloadAndFindToken(siteUrl, cmdLine.Token,"HTML Body", cmdLine.UserAgent, out html))
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

                                    Inform(String.Format("{0}{1}{2}", Tab, script_url, Environment.NewLine), 2);
                                    try
                                    {
                                        if (DownloadAndFindToken(script_url, cmdLine.Token,"JS File", cmdLine.UserAgent, out html))
                                        {
                                            break;
                                        }
                                    }
                                    catch (System.Exception)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Inform(String.Format("{0}Error loading {1}{2}", Tab, script_url, Environment.NewLine), 1);
                                        Console.ResetColor();
                                    }
                                }

                            }
                            doc = null; //release doc

                            #endregion

                        }
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
                { Console.WriteLine("{1}{1}{0} was not found on any of the sites searched:{1}", cmdLine.Token, Environment.NewLine); Console.ResetColor();}
                else
                {
                    Console.WriteLine("{2}{2}\"{0}\" was found on the following {1} sites:{2}", cmdLine.Token, findings.Count, Environment.NewLine); 
                    Console.ResetColor();
                    foreach (string foundit in findings.Keys)
                    {
                        Console.WriteLine("{0}{1}",Tab, foundit);
                    }

                }
                #endregion
            }
            catch (System.IO.FileNotFoundException exc)
            {
                Console.WriteLine("Error. Unable to open file {0}.{1}",  exc.Message, Environment.NewLine);
                
            }
            catch (Exception exc)
            {
                Console.WriteLine("Error: {0}{1}",exc.ToString(), Environment.NewLine);
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

        private static void RegisterFoundSite(string url, string excert)
        {
            try
            {
                findings.Add(url, excert);
            }
            catch(System.ArgumentException){}; //do nothing if it was already in the dictionary
            output.WriteLine(String.Format("{0}{2}{1}", url, excert, Environment.NewLine));
        }

        /// <summary>
        /// Download the html of a given url and check if it contains a token
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="outHTML"></param>
        /// <returns></returns>
        private static bool DownloadAndFindToken(string url,string token,string area, string userAgent, out string outHTML)
        {
            string excertHeaders=String.Empty;
            string excertBody = String.Empty;
            bool foundHeader=false, foundBody=false;

            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", userAgent);

            outHTML = string.Empty;

            //some urls have spaces and browsers support. Need to take spaces out of urls
            url=url.Replace(" ", "");
            try
            {
                outHTML = client.DownloadString(url);
            }
            catch (System.ArgumentException exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Inform(String.Format("\tError loading {0} : {1}{2}", url,exc.Message,Environment.NewLine), 1);
                Console.ResetColor();
                return false;
            }

            if (cmdLine.Headers) //Search HTTP headers ?
            {
                string headers = client.ResponseHeaders.ToString();
                foundHeader = regexToken.IsMatch(headers);
                if (foundHeader)
                {
                    //Show what was found
                    excertHeaders = ExtractMatches(headers, "HTTP Header", ConsoleColor.DarkYellow);


                }
            }
            
            foundBody = regexToken.IsMatch(outHTML);
            if (foundBody)
            {
                //Show what was found
                excertBody=ExtractMatches(outHTML,area,ConsoleColor.Green);
            }
            client = null; //release client

            //register what we found
            if (foundHeader || foundBody)
            {
                if (foundHeader)
                { RegisterFoundSite(url, string.Format("{2}HTTP Header:{0}{2}{1}",excertHeaders, excertBody, Environment.NewLine)); }
                else //found only in body
                { RegisterFoundSite(url, excertBody); }
            }
            return foundHeader||foundBody;
        }

        /// <summary>
        /// Extract RegEx matches in a way to show to user
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractMatches(string html,string area, ConsoleColor color)
        {
            StringBuilder sb = new StringBuilder();
            MatchCollection matches = regexToken.Matches(html);

            for (int i = 0; i < matches.Count; i++)
            {
                string substr = SubStringInform(html, matches[i].Index);
                //substr=Regex.Replace(substr, @"\s", "");
                substr = substr.Replace("\n", "").Replace("  ", "").Replace(Tab, "").Replace("\r", "");

                //is the match something "simple" to show to the user?
                if (cmdLine.Token.Contains(matches[i].ToString()))
                {
                    sb.AppendLine(Tab + (i + 1) + ": " + matches[i] + Tab + substr); 
                }
                else
                { 
                    sb.AppendLine(Tab + (i+1)  + ": " + cmdLine.Token + Tab + substr); 
                }
                //did we reach max results to show ?

                if (cmdLine.MaxResults != 0 && cmdLine.MaxResults <= i + 1) break;
            }
            

            Console.ForegroundColor = color;
            if (cmdLine.MaxResults > matches.Count)
            { Inform(Environment.NewLine + "Token Found in " + area+" in "+ matches.Count + " places:" + Environment.NewLine + sb.ToString(), 1); }
            else
            { Inform(Environment.NewLine + "Token Found in " + area + " in " + matches.Count + " places. Showing only " + cmdLine.MaxResults + Environment.NewLine + sb.ToString(), 1); }
            Console.ResetColor();
            Console.WriteLine();

            return sb.ToString();
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
        /// Opens a File, reads all lines and returns the contents split by return carriage
        /// </summary>
        /// <param name="response">The file to read</param>
		private static string[] GetSitesFromFile(string file) {
            var separator = new string[] {Environment.NewLine};
            string[] sites;
            using (var reader = new StreamReader(File.OpenRead(file)))
            {
                sites = reader.ReadToEnd().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            }

            return sites;
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
