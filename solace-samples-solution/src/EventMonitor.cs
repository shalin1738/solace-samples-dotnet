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
// 
//                * EventMonitor *
// This sample demonstrates monitoring appliance events using a relevant appliance 
// event subscription .
// 
// This sample requires:
// - A Session connected to a Solace appliance running SolOS-TR
// - The "Publish Client Event Messages" must be
//   enabled in the client's Message VPN on the appliance.
// 
// 
// In this sample, we subscribe to the appliance event topic for Client Connect
// events:
// 
//       #LOG/INFO/CLIENT/<appliance hostname>/CLIENT_CLIENT_CONNECT/>
// 
// With "Publish Client Event Messages" enabled for the Message VPN,
// all client events are published as messages. By subscribing to the above
// Topic, we are asking to receive all CLIENT_CLIENT_CONNECT event messages
// from the specified appliance.
// 
// Event Message Topics are treated as regular Topics in that wildcarding can be
// used in the same manner as typical topics. For example, if you want to
// receive all Client Events, regardless of Event Level, the following topic
// could be used:
// 
//      #LOG/*/CLIENT/<appliance hostname>/>
// 
// This sample triggers a CLIENT_CLIENT_CONNECT event by connecting a second
// time to the appliance (triggerSecondaryConnection()).
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using System.Xml;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class EventMonitor : SampleApp
    {
        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Demonstrates event monitoring over the message bus";
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
            sampleSpecificUsage = null;
            return true;
        }

        /// <summary>
        /// A message receive delegate to handle message events. 
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private static void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            IMessage msg = args.Message;
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("Topic: {0}\n",msg.Destination.Name));
            sb.Append(string.Format("Event: {0}\n",Encoding.UTF8.GetString(msg.BinaryAttachment)));
            Console.WriteLine(string.Format("Received: \n{0}\n", sb.ToString()));
            // It is recommended to dispose a received message to free up heap memory explicitly.
            msg.Dispose();
        }

        /// <summary>
        /// Triggers the connection of secondary Sessions.
        /// </summary>
        private void triggerSecondaryConnection(ArgParser cmdLineParser)  
        {
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            IContext context = null;
            ISession session = null;
            context = ContextFactory.Instance.CreateContext(contextProps, null);
		    ITopic dummyTopic = ContextFactory.Instance.CreateTopic("dummy/topic");
            session = context.CreateSession(sessionProps,
                SampleUtils.HandleMessageEvent,
                SampleUtils.HandleSessionEvent);
            session.Connect();
            session.Subscribe(dummyTopic,true);
            session.Dispose();
	    }

        /// <summary>
        /// The main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            #region Parse Arguments
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // parse failed
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            #endregion

            #region Initialize properties from command line
            // Initialize the properties
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define context and session
            IContext context = null;
            ISession session = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to connect to appliance. \n[Ensure selected message-vpn has 'Publish Client Event Messages' enabled ]");
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps,
                    HandleMessageEvent,
                    SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the session ...");

                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }

                // Build an event monitoring topic for client connect events and
                // subscribe to it.
                string routerHostname = (string) session.GetCapability(CapabilityType.PEER_ROUTER_NAME).Value.Value;
                string strEventTopic = string.Format("#LOG/INFO/CLIENT/{0}/CLIENT_CLIENT_CONNECT/>", routerHostname);
                ITopic eventTopic = ContextFactory.Instance.CreateTopic(strEventTopic);
                Console.WriteLine(string.Format("Adding subscription to '{0}'...", strEventTopic));
                try 
                {
                    if (session.Subscribe(eventTopic, true) == ReturnCode.SOLCLIENT_OK) 
                    {
                        Console.WriteLine("Successfully added event topic subscription");
                    }
                } 
                catch (OperationErrorException opex) 
                {
                    Console.WriteLine(string.Format("Failed to add susbscription to to event topic"));
                    PrintException(opex);
                }
                Console.WriteLine("Waiting to receive events ...");
                triggerSecondaryConnection(cmdLineParser);
                /* sleep to allow reception of event */
                Thread.Sleep(1000);
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
