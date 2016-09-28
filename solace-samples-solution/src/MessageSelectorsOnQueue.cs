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
//                    * MessageSelectorsOnQueue *
// This sample shows how to create a message flow to a Queue and how to use a 
// message selector to select which messages should be delivered.
//
// This sample will:
// - Create and bind a flow to a temporary Queue with a message selector on a 
//   user-defined property.
// - Publish a number of Guaranteed Delivery messages with the given 
//   user-defined property to the temporary Queue.
// - Show that, messages matching the registered selector are delivered to 
//   the temporary Queue flow.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;
using SolaceSystems.Solclient.Messaging.SDT;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class MessageSelectorsOnQueue : SampleApp
    {
        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Simple subscriber with selector";
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
            cmdLineParser.Config.DeliveryMode = MessageDeliveryMode.Persistent;
            cmdLineParser.Config.DestMode = DestMode.QUEUE;
            #endregion

            #region Initialize properties from command line
            // Initialize the properties
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext and ISession
            IContext context = null;
            ISession session = null;
            IFlow flow = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps, SampleUtils.HandleMessageEvent, SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }
                if (!session.IsCapable(CapabilityType.SELECTOR))
                {
                    Console.WriteLine(string.Format("Capability '{0}' is required to run this sample",
                        CapabilityType.SELECTOR));
                    return;
                }
                if (!session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED) || !(session.IsCapable(CapabilityType.TEMP_ENDPOINT))) 
                {
                    Console.WriteLine(string.Format("Capabilities '{0}'  and {1} are required to run this sample",
                        CapabilityType.SUB_FLOW_GUARANTEED,
                        CapabilityType.TEMP_ENDPOINT));
                    return;
                }

                // The creation of the Queue object. Temporary destinations must be
                // acquired from a connected session, as they require knowledge
                // about the connected appliance.
                IQueue queue = session.CreateTemporaryQueue();

                // The creation of a flow. A FlowReceiver is acquired for consuming
                // messages from a specified endpoint.
                // 
                // The selector "pasta = 'rotini' OR pasta = 'farfalle'" is used to
                // select only messages matching those pasta types in their user
                // property map.
                FlowProperties flowProps = new FlowProperties();
                flowProps.FlowStartState = true ; // created in a started state;
                flowProps.Selector = "pasta = 'rotini' OR pasta = 'farfalle'";
                flow = session.CreateFlow(flowProps,queue,null,SampleUtils.HandleMessageEvent,SampleUtils.HandleFlowEvent);

                // Now publish a number of messages to queue, the user should only get the ones with 'rotini' or 'farfalle'.
                // Note that this uses SDT and custom header properties, which could impact performance.
                IMessage message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                message.Destination = queue;
                string[] pastas = new string[] { "macaroni", "fettuccini", "farfalle", "fiori", "rotini", "penne" };
                for (int i = 0 ; i < pastas.Length; i++) 
                {
                    IMapContainer userProps = message.CreateUserPropertyMap();
                    userProps.AddString("pasta",pastas[i]);
                    if (session.Send(message) == ReturnCode.SOLCLIENT_OK)
                    {
                            Console.WriteLine(String.Format("- Sent {0}",pastas[i]));
                    }
                }
                Console.WriteLine(string.Format("\nDone\n Sleeping for {0} secs before exiting. Expecting 2 messages to match the selector ",Timeout/1000));
                Thread.Sleep(Timeout);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                if (flow != null) 
                {
                    flow.Dispose();
                }
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
