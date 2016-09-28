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
//                        * TopicToQueueMapping *
//
// This sample demonstrates the following: 
// * Provision a temporary Queue and add a couple of Topic subsriptions on it. 
// * Publish three messages, one to the Queue and a couple for each Topic.
// * Observe that all three messages end up on the Queue.
// * Next, remove one of the topic subscription from the Queue.
// * Publish the same messages as in the second step, observe only two messages are spooled on the Queue.
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
    public class TopicToQueueMapping : SampleApp
    {

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Topic to queue mapping";
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
            ITopic topicA = ContextFactory.Instance.CreateTopic("topicA");
            ITopic topicB = ContextFactory.Instance.CreateTopic("topicB");

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
                // Initialize the Context, connect the Session, and assert capabilities
                InitializeAndAssertCapabilities(ref context, ref session, sessionProps);

                // Provision a new Queue.
                string queueName = SampleUtils.SAMPLE_QUEUE + (new Random()).Next(1000);
                queue = ProvisionQueue(session, queueName, true);
          
                // Publish a couple of messages to topicA, topicB. Observe that none of them end up on the Queue.
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.DeliveryMode = MessageDeliveryMode.Persistent;
                msg.UserData = new byte[] { 1 };
                msg.Destination = topicA;
                session.Send(msg);
                msg.UserData = new byte[] {2};
                msg.Destination = topicB;
                session.Send(msg);
                DumpMessagesReceivedFromQueue(session, queue, 1000, true);

                // Add Topic subscriptions for topicA and topicB, resend the two messages. Observe that both of them end up on the Queue.
                session.Subscribe(queue, topicA, SubscribeFlag.WaitForConfirm, null);
                session.Subscribe(queue, topicB, SubscribeFlag.WaitForConfirm, null);
                msg.UserData = new byte[] { 1 };
                msg.Destination = topicA;
                session.Send(msg);
                msg.UserData = new byte[] { 2 };
                msg.Destination = topicB;
                session.Send(msg);
                DumpMessagesReceivedFromQueue(session, queue, 1000, true);

                // Remove TopicA subscription and resend the two messages. Observe that only TopicB message end up on the Queue.
                session.Unsubscribe(queue, topicA, SubscribeFlag.WaitForConfirm, null);
                msg.UserData = new byte[] { 1 };
                msg.Destination = topicA;
                session.Send(msg);
                msg.UserData = new byte[] { 2 };
                msg.Destination = topicB;
                session.Send(msg);
                DumpMessagesReceivedFromQueue(session, queue, 1000, true);

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

        // To hold the messages received from the local callback delegate.
        private IList<IMessage> _deliveredMessages = new List<IMessage>();

        /// <summary>
        /// Delivered messages callback delegate.
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
        /// Dump messages received from a given queue.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="queue"></param>
        /// <param name="waitTimeout"></param>
        /// <param name="autoack">If true messages will be automaticaly acked</param>
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
            // Clear messages on the shared message list '_deliveredMessages' first
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
            // Set permissions to allow all permissions to others.
            endpointProps.Permission = EndpointProperties.EndpointPermission.Delete;
            // Set access type to exclusive.
            endpointProps.AccessType = EndpointProperties.EndpointAccessType.Exclusive;
            // Set quota to 100 MB.
            endpointProps.Quota = 100;
            // Set respects TTL to respectsMsgTTL.
            endpointProps.RespectsMsgTTL = respectsMsgTTL;
            IQueue queue = ContextFactory.Instance.CreateQueue(queueName);
            Console.WriteLine(String.Format("About to provision queue '{0}' on the appliance", queueName));
            session.Provision(queue /* endpoint */,
                endpointProps /*endpoint properties */,
                ProvisionFlag.WaitForConfirm | ProvisionFlag.IgnoreErrorIfEndpointAlreadyExists /* block waiting for confirmation */,
                null /*no correlation key*/);
            Console.WriteLine(string.Format("Queue '{0}' successfully provisioned on the appliance", queueName));
            return queue;
        }

        /// <summary>
        /// Initialize Context and Session, and assert capabilities.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="session"></param>
        /// <param name="sessionProps"></param>
        private void InitializeAndAssertCapabilities(ref IContext context, ref ISession session, SessionProperties sessionProps)
        {
            Console.WriteLine("About to create the Context ...");
            context = ContextFactory.Instance.CreateContext(new ContextProperties(), null);
            Console.WriteLine("Context successfully created ");
            Console.WriteLine("About to create a Session  ...");
            sessionProps.CalculateMessageExpiration = false;
            sessionProps.CalculateMessageExpiration = true;
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
            // Does the appliance support these capabilities:PUB_GUARANTEED,ENDPOINT_MANAGEMENT and ENDPOINT_MESSAGE_TTL
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
            Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.QUEUE_SUBSCRIPTIONS));
            if (!session.IsCapable(CapabilityType.QUEUE_SUBSCRIPTIONS))
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
