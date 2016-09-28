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
//                * SempGetOverMB *
// Demonstrates simple SEMP requests over the Message Bus. It performs a request
// and prints the response.
//
// Sample requirements:
//  - A Solace appliance running SolOS-TR.
//  - When running with SolOS-TR 5.3.1 and above, the client's message vpn must have semp-over-msgbus enabled.
//  - The client's message vpn must have management-message-vpn enabled to send SEMP requests outside of its message vpn.
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System.Xml;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class SempGetOverMB : SampleApp
    {
        // SEMP schema version.
        private string SOLTR_VERSION = "5_4";

        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Sample using SEMP over the message bus";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = "\n\t [-vo] Verbose off: do not print SEMP protocol messages on the console. Default: vo not enabled";
            extraOptionsForCommonArgs += "\n\t [-sv SEMP_VERSION]  SEMP version in the SEMP request. Default: " + SOLTR_VERSION;
            sampleSpecificUsage = null;
            return true;
        }


        /// <summary>
        /// Main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            #region Parse Arguments
            string routerName = null;
            bool verbose = true;
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            if (cmdLineParser.Config.ArgBag.ContainsKey("-vo"))
            {
                verbose = false;
            }
            if (cmdLineParser.Config.ArgBag.ContainsKey("-sv"))
            {
                SOLTR_VERSION = cmdLineParser.Config.ArgBag["-sv"];
            }
            #endregion

            #region Initialize properties from command line.
            // Initialize the properties
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define Context and Session.
            IContext context = null;
            ISession session = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to connect to appliance. \n[Ensure selected message-vpn is configured as the appliance's management message-vpn.]");
		Console.WriteLine("[Ensure selected message-vpn has semp-over-msgbus enabled]");
                Console.WriteLine("About to create the Context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the Session ...");
                session = context.CreateSession(sessionProps,
                    SampleUtils.HandleMessageEvent,
                    SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the Session ...");

                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }

                // The SEMP requestStr topic is built using the appliance name.
                // 
                // The SEMP requestStr we perform asks to show the client's on the appliance.
                // It can be easily adapted to perform any other type
                // of SEMP show commands.
                ICapability cap_routerName = session.GetCapability(CapabilityType.PEER_ROUTER_NAME);
                if (cap_routerName.Value == null || cap_routerName.Value.Value.Equals(""))
                {
                    Console.WriteLine("Unable to load PEER_ROUTER_NAME. (Requires r4.6+ SolOS-TR appliance.)");
                    return;
                }
                // PEER_ROUTER_NAME is an ISDTField of type string.
                routerName = (string) cap_routerName.Value.Value;

			    string SEMP_TOPIC_STRING = string.Format("#SEMP/{0}/SHOW", routerName);
                Console.WriteLine(string.Format("Loaded appliance name: '{0}', SEMP Topic: '{1}'", routerName, SEMP_TOPIC_STRING));
			    ITopic SEMP_TOPIC = ContextFactory.Instance.CreateTopic(SEMP_TOPIC_STRING);
                string SEMP_SHOW_CLIENT_NAME = "<rpc semp-version=\"soltr/" + SOLTR_VERSION +
                    "\"><show><client><name>*</name></client></show></rpc>";

			    // Make the request.
                IMessage requestMsg = ContextFactory.Instance.CreateMessage();
                requestMsg.Destination = SEMP_TOPIC;
                requestMsg.BinaryAttachment = Encoding.UTF8.GetBytes(SEMP_SHOW_CLIENT_NAME);
                if (verbose)
                {
                    Console.WriteLine("REQUEST: " + SEMP_SHOW_CLIENT_NAME); // triggered by -v
                }
			    // Make the requestStr.
                IMessage replyMsg;
                ReturnCode rc = session.SendRequest(requestMsg, out replyMsg, 5000);
                if (rc == ReturnCode.SOLCLIENT_FAIL) 
                {
                    Console.WriteLine("Failed to send a requestStr");
                    return;
                }
                byte[] binaryAttachment = null;
                if (replyMsg != null)
                {
                    binaryAttachment = replyMsg.BinaryAttachment;
                }
                else
                {
                    Console.WriteLine("Failed to receive a SEMP reply");
                    return;
                }
			    if (binaryAttachment != null) {
				    string replyStr = Encoding.UTF8.GetString(binaryAttachment);
                    // Is this user allowed to make such SEMP requestStr?
                    if (replyStr.IndexOf("permission-error") != -1)
                    {
                        Console.WriteLine("Permission error, aborting");
                        Console.WriteLine("Make sure SEMP over message bus SHOW commands are enabled for this VPN");
                        Console.WriteLine("REPLY: " + replyStr);
                        return;
                    }
                    if (verbose)
                    {
                        Console.WriteLine("REPLY: " + replyStr); // triggered by -v
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                }
                if (context != null)
                {
                    context.Dispose();
                }
                // Must cleanup after. 
                CleanupContext();
            }
        }
    }
}
