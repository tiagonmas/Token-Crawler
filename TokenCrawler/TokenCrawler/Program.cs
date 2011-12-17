using System;
using Plossum.CommandLine;
using System.Linq;
using System.IO;
using System.Net;
using HtmlAgilityPack;
using System.Collections.Generic;

namespace TokenCrawler
{
    class Program
    {
        static int VerboseLevel;

        static List<string> foundList;
 
        static int Main(string[] args)
        {
            #region CommandLineArgsProcessing
            CmdLineOptions cmdLine = new CmdLineOptions();
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
            VerboseLevel = cmdLine.Verbose;
            Console.WriteLine("Running tool with the following commands:");
            Console.WriteLine("Token: {0}", cmdLine.Token);
            Console.WriteLine("File: {0}", cmdLine.File);
            Console.WriteLine("Verbose Level: {0}\n", cmdLine.Verbose);
            
            #endregion

            #region ReadSitesFromFileAndCrawl

            try
            {
                foundList = new List<string>(); //List to store urls that contain the token

                foreach (string siteUrl in GetSitesFromFile(cmdLine.File))
                {
                    var url = PrepURL(siteUrl); //check for http
                    Inform(String.Format("\nCrawling {0} \n", url), 1);

                    //show something is happening if we are in verbose=0 mode
                    if (cmdLine.Verbose == 0) Console.Write(".");

                    try
                    {
                        string html;
                        if (!DownloadAndFindToken(url,cmdLine.Token,out html))
                        { //it was not found on the main HTML page, check all referred scripts

                            #region CheckInternalScripts
                            HtmlDocument doc = new HtmlDocument();
                            doc.LoadHtml(html);
                            
                            var head = doc.DocumentNode.Descendants().Where(n => n.Name == "head").FirstOrDefault();
                            foreach (var script in doc.DocumentNode.Descendants("script").ToArray()) 
                            {
                                string script_url=String.Empty;

                                //show something is happening if we are in verbose=0 mode
                                if (cmdLine.Verbose <2) Console.Write(".");

                                //Console.Write(script.Value);
                                HtmlAttribute att = script.Attributes["src"];
                                if (att!=null)
                                {
                                    //check the absolute path to the script
                                    if (att.Value.StartsWith("http")) script_url = att.Value;
                                    else if (att.Value.StartsWith("/")) script_url = url + att.Value;
                                    else script_url = url + "/" + att.Value; //TO DO: take care of relative urls

                                    Inform(String.Format("\t{0}\n", script_url), 2);
                                    try
                                    {
                                        if (DownloadAndFindToken(script_url, cmdLine.Token, out html))
                                        {
                                            foundList.Add(url);
                                            break;
                                        }
                                    }
                                    catch (System.Net.WebException)
                                    {
                                        Console.ForegroundColor = ConsoleColor.Red;
                                        Inform(String.Format("\tError loading {0}\n", script_url), 1);
                                        Console.ResetColor();
                                    }
                                }

                            }
                            doc = null; //release doc

                            #endregion

                        } else {foundList.Add(url);}

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
                Console.ForegroundColor = ConsoleColor.Blue;
                if (foundList.Count == 0)
                { Console.WriteLine(String.Format("\n\n{0} was not found on any of the sites searched:\n", cmdLine.Token)); Console.ResetColor();}
                else
                { 
                    Console.WriteLine(String.Format("\n\n\"{0}\" was found on the following {1} sites:\n", cmdLine.Token, foundList.Count)); 
                    Console.ResetColor();
                    foreach (string str in foundList)
                    {
                        Console.WriteLine("\t"+str);
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
        /// Download the html of a given url and check if it contains a token
        /// </summary>
        /// <param name="url"></param>
        /// <param name="token"></param>
        /// <param name="outHTML"></param>
        /// <returns></returns>
        private static bool DownloadAndFindToken(string url,string token, out string outHTML)
        {
            WebClient client = new WebClient();
            //some urls have spaces and browsers support. Need to take spaces out of urls
            url=url.Replace(" ", "");
            outHTML = client.DownloadString(url);
            int found = outHTML.IndexOf(token);
            if (found > -1)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Inform("Token Found: " + SubStringInform(outHTML, token), 1);
                Console.ResetColor();
                Console.Write("\n\n");
            }
            client = null; //release client
            return found > -1;
        }

        /// <summary>
        /// Get the substring of the original string that contains the token, in order to show where the token was found
        /// </summary>
        /// <param name="html">the original html</param>
        /// <param name="token">the token that was found</param>
        /// <param name="found">where the token was found</param>
        /// <returns></returns>
        private static string SubStringInform(string html, string token)
        {   const int subsize=50;  //desired size of substring to show
            html = html.Replace(Environment.NewLine, "");
            int found=html.IndexOf(token); //find it again because we took the \n
            if (found==0)
                if (html.Length>subsize)
                    return html.Substring(0,subsize);
                else return token;
            if (found + subsize >= html.Length)
                return html.Substring(found, html.Length-found);
            return html.Substring(found, subsize);
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
            if (level <= VerboseLevel) Console.Write(str);
        }
        #endregion
    }
}
