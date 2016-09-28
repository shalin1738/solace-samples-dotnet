#region Copyright & License
//
// Solace Systems Messaging API
// Copyright 2008-2016 Solace Systems, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to use and
// copy the Software, and to permit persons to whom the Software is furnished to
// do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// UNLESS STATED ELSEWHERE BETWEEN YOU AND SOLACE SYSTEMS, INC., THE SOFTWARE IS
// PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS
// BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
// SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// http://www.SolaceSystems.com
//
//                * SempHttpSetRequest *
// A SEMP sample that uses SEMP/HTTP to show the statistics on a Solace appliance.
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System.Xml;
using System.Net;
using System.IO;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class SempHttpSetRequest : SampleApp
    {
        // Using 5.1 SEMP schema version.
        private string SOLTR_VERSION = "5_1";

        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "SEMP sample using SEMP over HTTP";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = null;
            sampleSpecificUsage = "Common parameters:\n" +
            "\t-h HOST         Appliance Management IP address\n" +
            "\t-u USER         CLI user name\n" +
            "\t-w PASSWORD   CLI user password\n" +
            "\t[-v]  Print SEMP protocol messages on the console\n" +
            "\t[-sv SEMP_VERSION]  SEMP version in the SEMP request. Default: " + SOLTR_VERSION;
            return false;
        }

        private void SEMPSet_ShowStats(ArgParser cmdLineParser,bool verbose)
        {
            // SEMP set request
            string requestStr = " <rpc semp-version=\"soltr/" + SOLTR_VERSION + "\"> <show> <stats> <client/> </stats> </show> </rpc>";
            byte[] requestData = Encoding.UTF8.GetBytes(requestStr);
            // Create the HTTP request.
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create("http://" +
                cmdLineParser.Config.IpPort.ip +
                "/SEMP");
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.Credentials =
                new NetworkCredential(cmdLineParser.Config.RouterUserVpn.user, cmdLineParser.Config.UserPassword);
            request.GetRequestStream().Write(requestData, 0, requestData.Length);
            request.GetRequestStream().Close();
            if (verbose)
            {
                Console.WriteLine("REQUEST: " + requestStr);
            }
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            if (resp != null)
            {
                Stream respStream = resp.GetResponseStream();
                XmlDocument replyDoc = new XmlDocument();
                replyDoc.Load(respStream);
                if (verbose)
                {
                    Console.WriteLine("RESPONSE: " + replyDoc.InnerXml);
                }
                XmlNode result = replyDoc.SelectSingleNode("//execute-result/@code");
                if (result != null)
                {
                    if (!"ok".Equals(result.Value))
                    {
                        Console.WriteLine("execute-result was not ok");
                    }
                    else
                    {
                        Console.WriteLine("success");

                        XmlNodeList stats = replyDoc.SelectNodes("//show/stats/client/global/stats/*");
                        foreach (XmlNode node in stats)
                        {
                            string name = node.Name.Replace('-', ' ') + ':';
                            name = name.PadRight(45);
                            Console.WriteLine("{0}{1}", name, node.InnerText);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("ShowStats response does not contain result: response = " + replyDoc.InnerXml);
                }
            }
        }


        private void SEMPGet_GetVersion(ArgParser cmdLineParser,bool verbose)
        {
            // SEMP get request.
            string requestStr = " <rpc semp-version=\"soltr/" + SOLTR_VERSION + "\"> <show> <version> </version> </show> </rpc>";
            byte[] requestData = Encoding.UTF8.GetBytes(requestStr);
            // Create the HTTP request.
            HttpWebRequest request =
                (HttpWebRequest)WebRequest.Create("http://" +
                cmdLineParser.Config.IpPort.ip +
                "/SEMP");
            request.Method = "POST";
            request.ContentType = "text/xml";
            request.Credentials =
                new NetworkCredential(cmdLineParser.Config.RouterUserVpn.user, cmdLineParser.Config.UserPassword);
            request.GetRequestStream().Write(requestData, 0, requestData.Length);
            request.GetRequestStream().Close();
            if (verbose)
            {
                Console.WriteLine("REQUEST: " + requestStr);
            }
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            if (resp != null)
            {
                Stream respStream = resp.GetResponseStream();
                XmlDocument replyDoc = new XmlDocument();
                replyDoc.Load(respStream);
                if (verbose)
                {
                    Console.WriteLine("RESPONSE: " + replyDoc.InnerXml);
                }
                XmlNode result = replyDoc.SelectSingleNode("//execute-result/@code");
                if (result != null)
                {
                    if (!"ok".Equals(result.Value))
                    {
                        Console.WriteLine("execute-result was not ok");
                    }
                    else
                    {
                        Console.WriteLine("success");
                    }
                }
                else
                {
                    Console.WriteLine("getVersion response does not contain result: response = " + replyDoc.InnerXml);
                }
            }
        }

        /// <summary>
        /// Main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            try
            {
                #region Parse Arguments
                bool verbose = false;
                ArgParser cmdLineParser = new ArgParser();
                if (!cmdLineParser.Parse(args))
                {
                    // Parse failed.
                    PrintUsage(INVALID_ARGUMENTS_ERROR);
                    return;
                }
                if (cmdLineParser.Config.ArgBag.ContainsKey("-v"))
                {
                    verbose = true;
                }
                if (cmdLineParser.Config.ArgBag.ContainsKey("-sv"))
                {
                    SOLTR_VERSION = cmdLineParser.Config.ArgBag["-sv"];
                }
                #endregion
                SEMPGet_GetVersion(cmdLineParser,verbose);
                SEMPSet_ShowStats(cmdLineParser,verbose);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
            }
            
        }
    }
}
