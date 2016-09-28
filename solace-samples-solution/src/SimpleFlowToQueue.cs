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
//                    * SimpleFlowToQueue *
// This sample shows how to create message flows to Queues (temporary and non-temporary).
// It demonstrates:
//    - Binding to a Queue (temporary or non-temporary)
//    - Configuring client acknowledge mode. (see HandleMessageAndAck below)
//    - Acknowledging incoming messages.
//
// For the non-temporary queue, this sample requires that a non-temporary/durable queue
// called 'my_sample_queue' be provisioned on the appliance with at lest a 'Modify Topic'  
// permission.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class SimpleFlowToQueue : SampleApp
    {
        // Reference to the Flow used to receive the messages from the Queue.
        private IFlow flow = null;

        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Flow to a temporary or non-temporary Queue";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = "Extra arguments for this sample:\n" +
                   "\t[--durable]  Use durable endpoint (default: temporary)\n" +
                   "\t[-mn number of messages to publish (default: 10)] \n";
            sampleSpecificUsage = null;
            return true;
        }

        /// <summary>
        /// Parse the sample's extra command line arguments.
        /// </summary>
        /// <param name="args"></param>
        private bool SampleParseArgs(ArgParser cmdLineParser)
        {
            cmdLineParser.Config.NumberOfMessagesToPublish = NumberOfMessagesToPublish;
            cmdLineParser.Config.DeliveryMode = MessageDeliveryMode.Persistent;
            try
            {
                string strnumOfMessagesToPublish = cmdLineParser.Config.ArgBag["-mn"];
                int numOfMessagesToPublish = Convert.ToInt32(strnumOfMessagesToPublish);
                cmdLineParser.Config.NumberOfMessagesToPublish = numOfMessagesToPublish;
            }
            catch (KeyNotFoundException)
            {
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'mn' " + ex.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Demonstrates message acknoweldgment of received messages over the Flow.
        /// ClientAck on the Flow is enabled by setting flowProps.AckMode to MessageAckMode.ClientAck.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        internal void HandleMessageAndAck(Object source, MessageEventArgs args)
        {
            // Print the message.
            SampleUtils.PrintMessageEvent(source, args);

            // Acknowledge incoming message after it has been processed.
            if (flow != null)
            {
                flow.Ack(args.Message.ADMessageId);
            }

            // It is recommended to dispose the message after processing it.
            args.Message.Dispose();
        }

        /// <summary>
        /// Main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            #region Parse Arguments
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            if (!SampleParseArgs(cmdLineParser))
            {
                // Parse failed for sample's arguments.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            #endregion

            #region Initialize properties from command line
            // Initialize the properties.
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext and ISession.
            IContext context = null;
            ISession session = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps,
                    SampleUtils.HandleMessageEvent, 
                    SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }
                if (!session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED) || !(session.IsCapable(CapabilityType.TEMP_ENDPOINT))) 
                {
                    Console.WriteLine(string.Format("Capabilities '{0}' and '{1}' are required to run this sample",
                        CapabilityType.SUB_FLOW_GUARANTEED,
                        CapabilityType.TEMP_ENDPOINT));
                    return;
                }
                IQueue queue = null;
                if (cmdLineParser.Config.UseDurableEndpoint)
                {
                    Console.WriteLine(string.Format("A non-temporary queue with name '{0}' must be provisioned and accessible on the appliance within the same user's Message VPN)", SampleUtils.SAMPLE_QUEUE));
                    queue = ContextFactory.Instance.CreateQueue(SampleUtils.SAMPLE_QUEUE);
                } else {
                    Console.WriteLine("Creating a temporary queue");
                    queue = session.CreateTemporaryQueue();
                }
                FlowProperties flowProps = new FlowProperties();
                // The flow is created in a started state, so it is ready to receive messages.
                flowProps.FlowStartState = true ; 
                // ClientAck, which means that the received messages on the Flow 
                // must be explicitly acked, otherwise they will be redelivered to the client
                // when the Flow reconnects.
                // ClientAck was chosen simply to illustrate ClientAck and that clients 
                // can use AutoAck instead.
                flowProps.AckMode = MessageAckMode.ClientAck;

                flow = session.CreateFlow(flowProps,queue,null,
                    // Demonstrates explicit client acknoweldgment of received messages.
                    HandleMessageAndAck,
                    SampleUtils.HandleFlowEvent);
                // Now publish a number of messages to the Queue.
                IMessage message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                message.Destination = queue;
                Console.WriteLine(string.Format("About to send {0} message(s) ...", cmdLineParser.Config.NumberOfMessagesToPublish));

                // Send a number of messages to the Queue.
                for (int i = 0; i < cmdLineParser.Config.NumberOfMessagesToPublish; i++)
                {
                    if (session.Send(message) == ReturnCode.SOLCLIENT_OK)
                    {
                        Console.Write(".");
                    }
                    Thread.Sleep(1000); // Wait for 0.5 seconds
                }
                Console.WriteLine(string.Format("\nDone\n Sleeping for {0} secs before exiting ",Timeout/1000));
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
