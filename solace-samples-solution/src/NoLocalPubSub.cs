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
//                        * NoLocalPubSub *
//
// This sample demonstrates the use of the NoLocal Session and Flow property. With
// this property enabled, messages are not received on the publishing session, even with a 
// Topic or flow match.
//
// This sample will:
// - Create and connect a couple of Sesions: 'sessionA' and 'sessionB'. 'sessionB' has No Local delivery enabled.
// - Create a Flow 'flowA' to a Queue with No Local delivery enabled.
// - Subscribe to a Topic T for Direct messages on a Session 'sessionB' 
// - Publish a Direct message to Topic T from each Session; verify that it is not delivered locally.
// - Publish a message to the Queue on each Session; verify that it is not delivered locally.
//
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class NoLocalPubSub : SampleApp
    {
        // Resources used in this sample.
        IContext context = null;
        ISession sessionA = null, sessionB = null;
        int msgCounterForSessionA = 0, 
            msgCounterForSessionB = 0,
            msgCounterForFlowA = 0;
        object countersLock = new object();
        IFlow flowA = null;

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "No Local Pub/Sub Sample";
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

        public override void SampleCall(string[] args)
        { 
            // Parse arguments and initialize Session properties.
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            // Create Session properties from the command line options.
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);

            try
            {
                InitContext(cmdLineParser.LogLevel);
                
                // Create and connect 'sessionA' with No Local delivery set to false.
                sessionProps.NoLocal = false; // No Local is set to 'false' by default.
                InitializeAndAssertCapabilities(ref context, ref sessionA,"sessionA",sessionProps);

                // Create and connect 'sessionB' with No Local delivery set to true.
                sessionProps.NoLocal = true; // <-- this is how we set NoLocal at the session level
                InitializeAndAssertCapabilities(ref context, ref sessionB,"sessionB",sessionProps);

                // Create a Flow to a temporary Queue within sessionA.
                IQueue queue = sessionB.CreateTemporaryQueue();
                FlowProperties flowProps = new FlowProperties();
                flowProps.NoLocal = true; // <-- this is how we set NoLocal at the flow level
                flowProps.BindBlocking = true;
                flowA = sessionA.CreateFlow(flowProps,queue,null,HandleMessageEvent,SampleUtils.HandleFlowEvent);
                flowA.Start();

                // Add a Topic subscription to sessionB.
                ITopic topic = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
                sessionB.Subscribe(topic, true/*wait for confirm*/);

                // Publish a Direct message to Topic T from each Session; verify it is not delivered locally.
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.Destination = topic;
                msg.DeliveryMode = MessageDeliveryMode.Direct;
                // Send from 'sessionA'.
                Console.WriteLine(string.Format("\nSending a direct message to topic '{0}' from sessionA",topic.ToString()));
                sessionA.Send(msg);
                Thread.Sleep(500);
                PrintCounters();
                Console.WriteLine(string.Format("Expecting msgCounterForSessionB to be 1, it's '{0}'", msgCounterForSessionB));
                Console.WriteLine(string.Format("Expecting msgCounterForSessionA to be 0, it's '{0}'", msgCounterForSessionA));
                ResetCounters();
                // Send from 'sessionB'.
                Console.WriteLine(string.Format("\nSending a direct message to topic '{0}' from sessionB",topic.ToString()));
                sessionB.Send(msg);
                Thread.Sleep(500);
                PrintCounters();
                Console.WriteLine(string.Format("Expecting msgCounterForSessionA to be 0, it's '{0}'",msgCounterForSessionA));
                Console.WriteLine(string.Format("Expecting msgCounterForSessionB to be 0, it's '{0}'", msgCounterForSessionB));
                ResetCounters();


                // Publish a message to the Queue on each Session; verify it is not delivered locally.
                msg.Destination = queue;
                msg.DeliveryMode = MessageDeliveryMode.Persistent;
                // Send from 'sessionA'.
                Console.WriteLine(string.Format("\nSending a persistent message to queue '{0}' from sessionA",queue.ToString()));
                sessionA.Send(msg);
                Thread.Sleep(500);
                PrintCounters();
                Console.WriteLine(string.Format("Expecting msgCounterForFlowA to be 0, it's '{0}'",msgCounterForFlowA));
                ResetCounters();
                // Send from 'sessionB'.
                Console.WriteLine(string.Format("\nSending a persistent message to queue '{0}' from sessionB", queue.ToString()));
                sessionB.Send(msg);
                Thread.Sleep(500);
                PrintCounters();
                Console.WriteLine(string.Format("Expecting msgCounterForFlowA to be 1, it's '{0}'",msgCounterForFlowA));
                ResetCounters();
                Console.WriteLine("\nDone");
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
               
                if (flowA != null)
                {
                    flowA.Dispose();
                }
                if (sessionA != null)
                {
                    sessionA.Dispose();
                }
                if (context != null) 
                {
                    context.Dispose();
                }
                // Must cleanup after.
                CleanupContext();
            }
        }

        /// <summary>
        /// The delivered messages callback function.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            if (source == sessionA)
            {
                lock (countersLock)
                {
                    msgCounterForSessionA++;
                }
            }
            else if (source == sessionB)
            {
                lock (countersLock)
                {
                    msgCounterForSessionB++;
                }
            }
            else if (source == flowA)
            {
                lock (countersLock)
                {
                    msgCounterForFlowA++;
                }
            }
            args.Message.Dispose();
        }

        /// <summary>
        /// Initialize Context, Session, and assert capabilities.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="session"></param>
        /// <param name="sessionProps"></param>
        private void InitializeAndAssertCapabilities(ref IContext context, 
               ref ISession session,
               string sessionName,
               SessionProperties sessionProps)
        {
            if (context == null)
            {
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
                Console.WriteLine("Context successfully created ");
                Console.WriteLine("About to create sessionA  ...");
            }
            session = context.CreateSession(sessionProps,
                        HandleMessageEvent,
                        SampleUtils.HandleSessionEvent);
            Console.WriteLine(string.Format("'{0}' successfully created.", sessionName));
            Console.WriteLine(string.Format("About to connect '{0}' ...",sessionName));
            if (session.Connect() == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine(string.Format("'{0}' successfully connected", sessionName));
            } else {
                Exit("Failed to connect sessionA",-1);
            }
            // Check if the capability is enabled on the appliance.
            if (!sessionA.IsCapable(CapabilityType.NO_LOCAL))
            {
                Exit(string.Format("Capability '{0}' must be supported in order to run this sample",
                    CapabilityType.NO_LOCAL.ToString()),-1);
            }
        }

        /// <summary>
        /// Resets the msg counters to 0.
        /// </summary>
        private void ResetCounters()
        {
            lock (countersLock)
            {
                msgCounterForFlowA = msgCounterForSessionA = msgCounterForSessionB = 0;
            }
        }

        /// <summary>
        /// Prints the msg counters to the console.
        /// </summary>
        private void PrintCounters()
        {
            lock (countersLock)
            {
                Console.WriteLine(string.Format("msgCounterForSessionA='{0} msgCounterForSessionB='{1}' msgCounterForFlowA='{2}'",
                    msgCounterForSessionA,msgCounterForSessionB,msgCounterForFlowA));
            }
        }


        /// <summary>
        /// Exits the sample and prints a message.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exitCode"></param>
        private void Exit(string message, int exitCode) 
        {
            Console.WriteLine(message);
            if (context != null)
            {
                // This will dispose all other resources associated with the Context.
                context.Dispose(); 
            }
            Environment.Exit(exitCode);
        }
    }
}
