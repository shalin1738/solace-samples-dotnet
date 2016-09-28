#region Copyright & License
//
// Solace Systems Messaging API
// Copyright 2009-2016 Solace Systems, Inc.
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
//
//                        * AdPubAck *
// In this sample, Guaranteed Delivery publishing with handling of message 
// acknowledgements is shown. Guaranteed Delivery is also known Assured Delivery.
//
// To accomplish this, the publisher makes use of the CorrelationKey
// in each message (IMessage.CorrelationKey). 
//
// The publisher sets the CorrelationKey to a message ack message-ack-correlation record
// before sending. Then in the event callback delegate
// the publisher can process acknowledgements and rejections to determine if the 
// appliance accepted the Guaranteed message.
//
// Specifically in this sample, the publisher maintains a list of
// outstanding messages not yet acknowledged by the appliance. After sending,
// the publisher checks to see if any of the messages have been 
// acknowledged and if so, it frees the resources.
//
// In the event callback delegate, the CorrelationKey of SessionEventArgs references
// the corresponding message-ack-correlation record; it is updated with the acknowledgement 
// status of the message.
// 
// For simplicity, this sample treats both message acceptance and 
// rejection the same way, the message is freed. In real world 
// applications, the client should decide what to do in the failure
// scenario.
//
// The reason the message is not processed in the event callback delegate
// in this sample is because it is not possible to make blocking 
// calls from within the event callback. In general, it is often 
// simpler to send messages as blocking, as is done in the publish
// thread of this sample. So consequently if an application 
// wanted to resend rejected messages, it would have to avoid doing
// this in the callback or update the code to use non-blocking 
// sends. This sample chooses to avoid processing the message within
// the callback.
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
    public class AdPubAck : SampleApp
    {

        /// <summary>
        /// Represents a simple message record.
        /// </summary>
        class MessageRecord
        {
            private bool m_isAcked;
            private bool m_isAccepted;
            private Int64 m_messageId;
            private readonly IMessage m_message;

            public MessageRecord(IMessage message)
            {
                m_message = message;
                m_isAcked = false;
                m_isAccepted = false;
                m_messageId = -1;
            }

            public IMessage Message
            {
                get
                {
                    return m_message;
                }
            }

            public bool Acked
            {
                get
                {
                    return m_isAcked;
                }
                set
                {
                    m_isAcked = value;
                }
            }

            public bool Accepted
            {
                get
                {
                    return m_isAccepted;
                }
                set
                {
                    m_isAccepted = value;
                }
            }

            public Int64 MessageId
            {
                get
                {
                    return m_messageId;
                }
                set
                {
                    m_messageId = value;
                }
            }

        }

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "AD publisher with replication ";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool  GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
 	        extraOptionsForCommonArgs = "\t[-mn number of messages to publish (default: 1)] \n";
            sampleSpecificUsage = null;
            return true;
        }

        /// <summary>
        /// Parse the sample's extra command line arguments.
        /// </summary>
        /// <param name="args"></param>
        private bool SampleParseArgs(ArgParser cmdLineParser)
        {
            cmdLineParser.Config.DestMode = DestMode.TOPIC;
            cmdLineParser.Config.NumberOfMessagesToPublish = 1;
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
        /// Handle the session events.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public static void HandleSessionEvent(Object sender, SessionEventArgs args)
        {
            Console.WriteLine(string.Format("HandleSessionEvent - {0}", args.ToString()));
            switch (args.Event)
            {
                case SessionEvent.Acknowledgement:
                case SessionEvent.RejectedMessageError:
                    MessageRecord mr = args.CorrelationKey as MessageRecord;
                    if (mr != null) 
                    {
                        mr.Acked = true;
                        if (args.Event == SessionEvent.Acknowledgement) 
                        {
                            mr.Accepted = true;
                        } else if (args.Event == SessionEvent.RejectedMessageError) {
                            mr.Accepted  = false;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

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
            // Uncomment the line below if you wish to send using a non-blocking mode
            // sessionProps.SendBlocking = false; 
            #endregion

            // Define IContext and ISession.
            IContext context = null;
            ISession session = null;

            // Create the LinkedList.
            LinkedList<MessageRecord> msgRecords =
                new LinkedList<MessageRecord>();

            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");
                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps,
                            SampleUtils.HandleMessageEvent,
                            HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                // Connect the session.
                Console.WriteLine("About to connect the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }

                // Validate required capabilities.
                if (!session.IsCapable(CapabilityType.PUB_GUARANTEED))
                {
                    Console.WriteLine(string.Format("This sample requires capability '{0}' to be supported",CapabilityType.PUB_GUARANTEED));
                    return;
                }

                // Send cmdLineParser.Config.NumberOfMessagesToPublish messages.
                for (int i = 0; i < cmdLineParser.Config.NumberOfMessagesToPublish; i++)
                {
                    // Allocate a new message.
                    IMessage message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                    message.DeliveryMode = MessageDeliveryMode.Persistent;
                    try
                    {
                        // Create a record, and set it as CorrelationKey.
                        MessageRecord msgRecord = new MessageRecord(message);
                        message.CorrelationKey = msgRecord;
                        ReturnCode rc = session.Send(message);
                        Console.WriteLine("Sending message " + i  + ": " + rc);
                        if (rc == ReturnCode.SOLCLIENT_OK)
                        {
                            // Add it to the list of send message records and send it.
                            msgRecord.MessageId = i;
                            msgRecords.AddLast(msgRecord);
                        }
                        else
                        {
                            // The message was not sent, free it up
                            message.Dispose();
                        }
                    }
                    catch (OperationErrorException opex)
                    {
                        // Ignore OperationErrorException if you don't want the publisher 
                        // to abort on transient send errors
                        Console.WriteLine("Got an excpetion " + opex.ReturnCode);
                        message.Dispose();
                        continue;
                    }
                    // Sleep for 500 msecs and check to see if the message was acknowledged (positively or negatively).
                    Thread.Sleep(100);
                    while (msgRecords.First != null && msgRecords.First.Value.Acked)
                    {
                        MessageRecord record = msgRecords.First.Value;
                        msgRecords.RemoveFirst();
                        Console.WriteLine(
                            string.Format("Freeing memory for message {0}, Result: Acked ({1}), Accepted ({2})\n",
                            record.MessageId, record.Acked, record.Accepted));
                        record.Message.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                Thread.Sleep(3000);
                // There should not be any left in the list, but just in case.
                foreach (MessageRecord record in msgRecords)
                {
                    Console.WriteLine(
                        string.Format("Freeing memory for message {0}, Result: Acked ({1}), Accepted ({2})\n",
                        record.MessageId, record.Acked, record.Accepted));
                    record.Message.Dispose();
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
