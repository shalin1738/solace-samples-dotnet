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
//                    * ActiveFlowIndication *
// This sample shows how to create message flows to Topics with the active flow indication
// property enabled
// It demonstrates:
//    - The FlowActive event when a bound flow is initially active.
//    - The FlowActive event when a bound flow that was initially inactive become active.

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class ActiveFlowIndication : SampleApp
    {
        // Reference to the Flows for which this sample will receive Active Flow Indication events.
        private IFlow flow1 = null;
        private IFlow flow2 = null;

        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Demonstrate active flow indication events.";
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
        /// Parse the sample's extra command line arguments.
        /// </summary>
        /// <param name="args"></param>
        private bool SampleParseArgs(ArgParser cmdLineParser)
        {
            cmdLineParser.Config.DeliveryMode = MessageDeliveryMode.Persistent;
            cmdLineParser.Config.DestMode = DestMode.QUEUE;

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
            IQueue queue = null;
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

                #region Provision an exclusive queue
                EndpointProperties endpointProps = new EndpointProperties();
                // Set permissions to allow all permissions to others.
                endpointProps.Permission = EndpointProperties.EndpointPermission.Delete;
                // Set access type to exclusive. 
                endpointProps.AccessType = EndpointProperties.EndpointAccessType.Exclusive;
                // Set quota to 100 MB.
                endpointProps.Quota = 100;
                string queueName = "solclient_dotnet_sample_ActiveFlowIndication_" + (new Random()).Next(1000);

                queue = ContextFactory.Instance.CreateQueue(queueName);
                Console.WriteLine(String.Format("About to provision queue '{0}' on the appliance", queueName));
                try
                {
                    session.Provision(queue /* endpoint */,
                        endpointProps /*endpoint properties */,
                        ProvisionFlag.WaitForConfirm /* block waiting for confirmation */,
                        null /*no correlation key*/);
                    Console.WriteLine("Endpoint queue successfully provisioned on the appliance");
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                    Console.WriteLine("Exiting");
                    return;
                }
                #endregion

                FlowProperties flowProps = new FlowProperties();
                // The Flow is created in a started state, so it is ready to receive messages.
                flowProps.FlowStartState = true;
                // AutoAck means that the received messages on the Flow 
                // will be implicitly acked on return from the message event handler
                // specified in CreateFlow().
                flowProps.AckMode = MessageAckMode.AutoAck;
                //Activate the active flow indication events
                flowProps.ActiveFlowInd = true;

                EventWaitHandle waitForFlowActiveEvent = new AutoResetEvent(false);

                flow1 = session.CreateFlow(flowProps, queue, null,
                    SampleUtils.HandleMessageEvent,
                    new EventHandler<FlowEventArgs>(
                                        delegate(object source, FlowEventArgs evt)
                                        {
                                            switch(evt.Event)
                                            {
                                                case FlowEvent.FlowActive:
                                                    Console.Out.WriteLine("Flow 1 Active event received");
                                                    waitForFlowActiveEvent.Set();
                                                break;
                                            }
                                        }));

                if (waitForFlowActiveEvent.WaitOne(5000, false))
                {
                    waitForFlowActiveEvent.Reset();
                }
                else
                {
                    // We did not get a FlowEvent.UpNotice within five seconds.
                    Console.Out.WriteLine("Did not get a FlowEvent.FlowActive for flow 1 within 5 secs, exiting ...");
                    return;
                }

                flow2 = session.CreateFlow(flowProps, queue, null,
                    SampleUtils.HandleMessageEvent,
                    new EventHandler<FlowEventArgs>(
                        delegate(object source, FlowEventArgs evt)
                        {
                            switch (evt.Event)
                            {
                                case FlowEvent.FlowInactive:
                                    Console.Out.WriteLine("Flow 2 Inactive event received");
                                    break;
                                case FlowEvent.FlowActive:
                                    Console.Out.WriteLine("Flow 2 Active event received");
                                    waitForFlowActiveEvent.Set();
                                    break;
                            }
                        }));
                Console.WriteLine("Flow 2 started.");

                Console.WriteLine("Stopping flow 1.");
                if (flow1.Stop() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Flow 1 stopped.");
                }
                else
                {
                    Console.WriteLine("Failure while stopping flow 1.  Exiting ...");
                    return;
                }
                Console.WriteLine("Disposing of flow 1");
                flow1.Dispose();
                flow1 = null;
                Console.WriteLine("Flow 1 has been disposed");
                if (waitForFlowActiveEvent.WaitOne(5000, false))
                {
                    waitForFlowActiveEvent.Reset();
                }
                else
                {
                    // We did not get a FlowEvent.UpNotice within five seconds.
                    Console.Out.WriteLine("Did not get a FlowEvent.FlowActive for flow 2 within 5 secs, exiting ...");
                    return;
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
                if (flow1 != null) 
                {
                    flow1.Dispose();
                }
                if (flow2 != null)
                {
                    flow2.Dispose();
                }
                if (queue != null)
                {
                    if (session != null)
                    {
                        session.Deprovision(queue, ProvisionFlag.WaitForConfirm, null);
                    }
                    queue.Dispose();
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
