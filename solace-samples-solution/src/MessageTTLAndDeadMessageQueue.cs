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
//                        * MessageTTLAndDeadMessageQueue *
//
// This sample demonstrates the following: 
// * How to provision a Dead Message Queue (DMQ) if one does not already exist for the given Message VPN.
// * How to provision a Queue which respects TimeToLive (TTL). 
// * How to use TimeToLive property on IMessage to demonstrate message expiry.
//     The sample will pause twice for two seconds, showing that messages
//     that expire before and after each interval end up in the right place.
// * How to use Expiration with or without specifing TimeToLive.
// * How to look at messages on the DMQ.
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
    public class MessageTTLAndDeadMessageQueue : SampleApp
    {

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Message TTL, Expiration and DMQ";
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
            // Resources used in this sample.
            IContext context = null;
            ISession session = null;
            IQueue queue = null;
            IQueue dmq = null; // the DMQ

            // Parse arguments and initialize Session properties.
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);

            try
            {
                InitContext(cmdLineParser.LogLevel);
                // Initialize the Context, connect the Session, and assert capabilities.
                InitializeAndAssertCapabilities(ref context, ref session, sessionProps);

                // Provision a new Queue and #DEAD_MSG if it does not already exist.
                string queueName = SampleUtils.SAMPLE_QUEUE + (new Random()).Next(1000);
                queue = ProvisionQueue(session, queueName, true);
                dmq = ProvisionQueue(session, "#DEAD_MSG_QUEUE", false);

                // Publish a couple of messages to the Queue.
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.DeliveryMode = MessageDeliveryMode.Persistent;
                msg.Destination = queue;
                // Send three messages:
                // message one (1):
                // TimeToLive = 2000 (2 secs) and DMQEligible is true 
                // In this case, when the message expires, it will be moved to the DMQ
                msg.TimeToLive = 2000;
                msg.DMQEligible = true;
                msg.UserData = new byte[] { (byte)1 };
                session.Send(msg);

                // Send three messages:
                // message two (2):
                // TimeToLive = 5000 (5 secs) and DMQEligible is false 
                // In this case, when the message expires, it will be deleted from the Queue,
                // but it will not end up on the DMQ.
                msg.TimeToLive = 5000;
                msg.DMQEligible = false;
                msg.UserData = new byte[] { (byte)2 };
                session.Send(msg);
                long expiration = msg.Expiration;

                // message three (3):
                // TimeToLive = 0 , DMQEligible is true and Expiration=(within 5 secs of the current time)
                msg.TimeToLive = 0;
                msg.Expiration = expiration;
                msg.UserData = new byte[] {(byte)3};
                session.Send(msg);

                // Start a flow to the Queue and verify that all three messages are there.
                DumpMessagesReceivedFromQueue(session, queue, 1000, false);

                // Wait for two seconds.
                Console.WriteLine("\n\nWaiting for 2 secs\n\n");
                Thread.Sleep(2000);

                // Start a flow to queue and verify that message one is no longer there, and 
				// messages two and three are still there. 
                DumpMessagesReceivedFromQueue(session, queue, 1000, false);

                // Wait for two seconds.
                Console.WriteLine("\n\nWaiting for 2 more secs\n\n");
                Thread.Sleep(2000);

                // Start a flow to the Queue and verify that message two is no longer there, 
				// but message 3 is still there.
                DumpMessagesReceivedFromQueue(session, queue, 1000, false);

                // Start a flow to the DMQ and verify that message one (who has DMQEligible=true) 
				// is there, but not message two or three.
                DumpMessagesReceivedFromQueue(session, dmq, 1000, true);

                Console.WriteLine("\nDone");
            }
            catch (Exception ex)
            {
                PrintException(ex);
                Console.WriteLine("Exiting");
            }
            finally
            {
                if (session != null)
                {
                    session.Deprovision(queue, ProvisionFlag.WaitForConfirm, null);
                    session.Deprovision(dmq, ProvisionFlag.WaitForConfirm, null);
                    session.Dispose();
                }
                if (context != null)
                {
                    context.Dispose();
                }
                // Must cleanup after 
                CleanupContext();
            }
        }

        // To hold the messages received from the local callback delegate.
        private IList<IMessage> _deliveredMessages = new List<IMessage>();

        /// <summary>
        /// The Delivered messages callback delegate.
        /// To keep the sample simple, messages are added to _deliveredMessages.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            lock (_deliveredMessages)
            {
                _deliveredMessages.Add(args.Message);
            }
        }

        /// <summary>
        /// Dump messages received from a given Queue.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="queue"></param>
        /// <param name="waitTimeout"></param>
        /// <param name="autoack">If true, messages will be automaticaly acked</param>
        private void DumpMessagesReceivedFromQueue(ISession session, IQueue queue, int waitTimeout, bool autoack)
        {
            FlowProperties flowProps = new FlowProperties();
            if (autoack)
            {
                flowProps.AckMode = MessageAckMode.AutoAck;
            }
            else
            {
                flowProps.AckMode = MessageAckMode.ClientAck;
            }
            // Clear messages on the shared message list '_deliveredMessages' first.
            lock (_deliveredMessages)
            {
                _deliveredMessages.Clear();
            }
            IFlow flow = session.CreateFlow(flowProps, queue, null, HandleMessageEvent, SampleUtils.HandleFlowEvent);
            // To keep the sample simple, it is good enough to use Thread.Sleep().
            Thread.Sleep(waitTimeout);
            // Then dispose.
            flow.Dispose();
            // Dump the received messages.
            Console.WriteLine(string.Format("\n@@@ Received {0} message(s) over queue {1}", _deliveredMessages.Count, queue.ToString()));
            foreach (IMessage deliveredMsg in _deliveredMessages)
            {
                Console.WriteLine(deliveredMsg.Dump()+"\n");
            }
        }


        /// <summary>
        /// Provision a Queue on appliance if one with the same name and Message VPN
        /// already exists.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="queueName"></param>
        /// <param name="respectsMsgTTL"></param>
        private IQueue ProvisionQueue(ISession session, string queueName, bool respectsMsgTTL)
        {
            EndpointProperties endpointProps = new EndpointProperties();
            // Set permissions to allow all permissions to others
            endpointProps.Permission = EndpointProperties.EndpointPermission.Delete;
            // Set access type to exclusive 
            endpointProps.AccessType = EndpointProperties.EndpointAccessType.Exclusive;
            // Set quota to 100 MB
            endpointProps.Quota = 100;
            // Set respects TTL to respectsMsgTTL
            endpointProps.RespectsMsgTTL = respectsMsgTTL;

            IQueue queue = ContextFactory.Instance.CreateQueue(queueName);
            session.Provision(queue /* endpoint */,
                endpointProps /*endpoint properties */,
                ProvisionFlag.WaitForConfirm | ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists /* block waiting for confirmation */,
                null /*no correlation key*/);
            Console.WriteLine(string.Format("Queue '{0}' successfully provisioned on the appliance", queueName));
            return queue;
        }

        /// <summary>
        /// Initialize the Context, Session, and assert capabilities.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="session"></param>
        /// <param name="sessionProps"></param>
        private void InitializeAndAssertCapabilities(ref IContext context, ref ISession session, SessionProperties sessionProps)
        {
            Console.WriteLine("About to create the context ...");
            context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
            Console.WriteLine("Context successfully created ");
            Console.WriteLine("About to create a session  ...");
            sessionProps.CalculateMessageExpiration = false;
            sessionProps.CalculateMessageExpiration = true;
            session = context.CreateSession(sessionProps,
                        this.HandleMessageEvent,
                        SampleUtils.HandleSessionEvent);
            Console.WriteLine("Session successfully created.");
            Console.WriteLine("About to connect the session ...");
            if (session.Connect() == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine("Session successfully connected");
                Console.WriteLine(GetRouterInfo(session));
            }
            // Does the appliance support these capabilities:PUB_GUARANTEED,ENDPOINT_MANAGEMENT and ENDPOINT_MESSAGE_TTL?
            Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.PUB_GUARANTEED));
            if (!session.IsCapable(CapabilityType.PUB_GUARANTEED))
            {
                Console.WriteLine("Not Supported\n Exiting");
                return;
            }
            else
            {
                Console.WriteLine("Supported");
            }
            Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.ENDPOINT_MANAGEMENT));
            if (!session.IsCapable(CapabilityType.ENDPOINT_MANAGEMENT))
            {
                Console.WriteLine("Not Supported\n Exiting");
                return;
            }
            else
            {
                Console.WriteLine("Supported");
            }
            Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.ENDPOINT_MESSAGE_TTL));
            if (!session.IsCapable(CapabilityType.ENDPOINT_MESSAGE_TTL))
            {
                Console.WriteLine("Not Supported \n Exiting");
                return;
            }
            else
            {
                Console.WriteLine("Supported");
            }
        }
    }
}
