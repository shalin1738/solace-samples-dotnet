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
//                    * SimpleFlowToTopic *
// This sample shows how to create message flows to Topics (temporary and non-temporary).
// It demonstrates:
//    - Binding to a Topic Endpoint (durable or non-durable/temporary)
//    - Configuring auto-acknowledge mode.
//
// To bind to a durable Topic Endpoint, this sample requires that a durable Topic Endpoint
// called 'my_sample_topicendpoint' must be provisioned on the appliance with at least 'Modify Topic' permission.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class SimpleFlowToTopic : SampleApp
    {
        // Reference to the Flow used to receive the messages from the Topic Endpoint.
        private IFlow flow = null;

        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Simple flow to a temporary or non-temporary Topic.";
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
            cmdLineParser.Config.DestMode = DestMode.QUEUE;
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
        /// Main function in the sample.
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
            if (!SampleParseArgs(cmdLineParser))
            {
                // parse failed for sample's arguments
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
            ITopicEndpoint topicEndpoint = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
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
                if (!session.IsCapable(CapabilityType.SUB_FLOW_GUARANTEED) || !(session.IsCapable(CapabilityType.TEMP_ENDPOINT))) 
                {
                    Console.WriteLine(string.Format("Capabilities '{0}' and '{1}' are required to run this sample",
                        CapabilityType.SUB_FLOW_GUARANTEED,
                        CapabilityType.TEMP_ENDPOINT));
                    return;
                }
                ITopic topic = null;
                if (cmdLineParser.Config.UseDurableEndpoint)
                {
                    Console.WriteLine(string.Format("A durable topic endpoint with name '{0}' must be provisioned and accessible on the appliance in the same user's Message VPN.)", SampleUtils.SAMPLE_TOPICENDPOINT));
                    topicEndpoint = ContextFactory.Instance.CreateDurableTopicEndpointEx(SampleUtils.SAMPLE_TOPICENDPOINT);
                    topic = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
                } else {
                    Console.WriteLine("Creating a temporary Topic");
                    topic = session.CreateTemporaryTopic();
                    topicEndpoint = session.CreateNonDurableTopicEndpoint();
                }
                FlowProperties flowProps = new FlowProperties();
                // The Flow is created in a started state, so it is ready to receive messages.
                flowProps.FlowStartState = true;
                // AutoAck means that the received messages on the Flow 
                // will be implicitly acked on return from the message event handler
                // specified in CreateFlow().
                flowProps.AckMode = MessageAckMode.AutoAck;
                // NON-BLOCKING FLOW CREATE: make sure that the flowProps.BindBlocking is set to false;
                flowProps.BindBlocking = false;

                // NON-BLOCKING FLOW CREATE: to demonstrate waiting on flow up event
                EventWaitHandle waitForFlowUpEvent = new AutoResetEvent(false);

                flow = session.CreateFlow(flowProps,topicEndpoint,topic,
                    SampleUtils.HandleMessageEvent,
                    new EventHandler<FlowEventArgs>(
                                        delegate(object source, FlowEventArgs evt)
                                        {
                                            if (evt.Event == FlowEvent.UpNotice)
                                            {
                                                waitForFlowUpEvent.Set();
                                            }
                                        }));

                if (waitForFlowUpEvent.WaitOne(5000, false))
                {
                    // We got a FlowEvent.UpNotice.
                    Console.Out.WriteLine("Flow created, we can proceed now");
                }
                else
                {
                    // We did not get a FlowEvent.UpNotice within five seconds.
                    Console.Out.WriteLine("Did not get a FlowEvent.UpNotice within 5 secs, exiting ...");
                    return;
                }
                // Send a number of messages to the Topic.
                IMessage message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                message.Destination = topic;
                Console.WriteLine(string.Format("About to send {0} messages ...", cmdLineParser.Config.NumberOfMessagesToPublish));
                for (int i = 0; i < cmdLineParser.Config.NumberOfMessagesToPublish; i++)
                {
                    if (session.Send(message) == ReturnCode.SOLCLIENT_OK)
                    {
                        Console.Write(".");
                    }
                    Thread.Sleep(1000); // wait for 1.0 seconds.
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
                bool flowWasConnected = (flow != null);
                if (flow != null) 
                {
                    flow.Dispose();
                }
                // Durable Topic Endpoints will continue getting messages on the registered Topic
                // subscription if client applications do not unsubscribe.
                // Non-durable Topic Endpoints will be cleaned up automatically after client applications
                // dispose the Flows bound to them.
                //
                // The following code block demonstrates how to unsubscribe or remove a subscribed Topic on 
                // the durable Topic Endpoint.
                // Two conditions must be met:
                // - The durable Topic Endpoint must have at least 'Modify Topic' permission enabled.
                // - No flows are currently bound to the durable Topic Endpoint in question.
                if (topicEndpoint != null && topicEndpoint.Durable && session != null && flowWasConnected)
                {
                    Console.WriteLine(string.Format("About to unsubscribe from durable Topic Endpoint '{0}'", ((ITopicEndpoint)topicEndpoint).Name));
                    session.Unsubscribe(topicEndpoint, "Unsubscribe Operation Correlation ID");
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
