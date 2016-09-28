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
//                * Replication *
// This sample shows how the publishing of Guaranteed messages works and how message 
// acknowledgements are handled for the Replication feature.
//
// The Replication feature allows GM clients connected to a Virtual Router located
// at a particular location (i.e. the Active Replication site) to be disconnected
// due to either a planned or unplanned site outage, then reconnect to a different
// Virtual Router at a different location (i.e. the Standby Replication site), with
// no message loss.
// 
// To accomplish this, the publisher makes use of the CorrelationKey
// in each message. The publisher adds a reference to a MessageRecord
// to the Solace message before sending. Then in the event callback delegate, 
// the publisher processes SessionEvent.Acknowledgement and 
// SessionEvent.RejectedMessageError to determine if the 
// appliance accepted the Guaranteed message.
//
// In this specific sample, the publisher maintains a linked list of
// outstanding messages not yet acknowledged by the appliance. After sending,
// the publisher checks to see if any of the messages have been 
// acknowledged, and, if so, it frees the resources.
//
// In the event callback, the original reference to the MessageRecord object
// is passed in as an argument, and the event callback updates the information 
// to indicate if the message has been acknowledged.  
// 
// For simplicity, this sample treats both message acceptance and 
// rejection the same way: the message is freed. In real world 
// applications, the client should decide what to do in the failure
// scenario.
//
// The reason the message is not processed in the event callback 
// in this sample is because it is not possible to make blocking 
// calls from within the event callback. In general, it is often 
// simpler to send messages as blocking, as is done in the publish
// thread of this sample. So, consequently, if an application 
// wanted to resend rejected messages, it would have to avoid doing
// this in the callback or update the code to use non-blocking 
// sends. This sample chooses to avoid processing the message within
// the callback.
//
//
// Sample Requirements:
//  - Solace appliance running SolOS-TR which replication support 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class Replication : SampleApp
    {
        // To flag if a session has received a session down errror with unknown flow name 
        private bool sessionIsDisconnected = false;

        // To flag if a session is reconnecting
        //
        // This is necessary to avoid sending messages while the session is
        // reconnecing and therefore it might cause out of order.
        //
        // To avoid such cases the applications should not send messages while their
        // sesion is in that state.
        private bool sessionIsReconnecting = false;


        /// <summary>
        /// Represents a simple message record.
        /// </summary>
        class MessageRecord
        {
            private bool m_isAcked;
            private readonly IMessage m_message;

            public MessageRecord(IMessage message)
            {
                m_message = message;
                m_isAcked = false;
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
        }

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "AD publisher with replication";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = "\t[-mn number of messages to publish (default: 50)] \n";
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
            cmdLineParser.Config.NumberOfMessagesToPublish = 50;
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
        private void HandleSessionEvent(Object sender, SessionEventArgs args)
        {
            Console.WriteLine(string.Format("HandleSessionEvent - {0}", args.ToString()));
            switch (args.Event)
            {
                case SessionEvent.Acknowledgement:
                case SessionEvent.RejectedMessageError:
                    MessageRecord mr = args.CorrelationKey as MessageRecord;
                    if (mr != null) 
                    {
                        // positively or negatively, we don't distinguish between the cases here
                        // for the the sake of sample simplicity
                        mr.Acked = true; 
                    }
                    break;
                case SessionEvent.DownError:
                    // Check to see if the subcode is of type "Unknow Flow Name", this will indicate
                    // a loss of publisher state
                    SDKErrorInfo lastSDKError = ContextFactory.Instance.GetLastSDKErrorInfo();
                    if (lastSDKError.SubCode.Equals(SDKErrorSubcode.UnknownFlowName))
                    {
                        sessionIsDisconnected = true;
                        Console.WriteLine(
                            string.Format(
                            "Received DownError event with SDKErrorInfo.SubCode={0} , the session will not automaticallly reconnect",
                            lastSDKError.SubCode)
                            );
                    }
                    else
                    {
                        Console.WriteLine(
                            string.Format(
                            "Received DownError event with SDKErrorInfo.SubCode={0}",
                            lastSDKError.SubCode)
                            );
                    }
                    sessionIsReconnecting = false;
                    break;
                case SessionEvent.Reconnecting:
                    sessionIsReconnecting = true;
                    sessionIsDisconnected = true;
                    break;
                case SessionEvent.Reconnected:
                    sessionIsReconnecting = false;
                    sessionIsDisconnected = false;
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
                else
                {
                    Console.WriteLine("Failed to connect session, aborting ...");
                    return;
                }

                // Validate required capabilities.
                if (!session.IsCapable(CapabilityType.PUB_GUARANTEED))
                {
                    Console.WriteLine(string.Format("This sample requires capability '{0}' to be supported",CapabilityType.PUB_GUARANTEED));
                    return;
                }

                // At this point the session is connected and not reconnecting
                sessionIsReconnecting = false;
                sessionIsReconnecting = false;

                // Send cmdLineParser.Config.NumberOfMessagesToPublish messages.
                for (int i = 0; i < cmdLineParser.Config.NumberOfMessagesToPublish; i++)
                {
                    // If the session is reconnecting the applications should not send messages
                    while (sessionIsReconnecting)
                    {
                        Thread.Sleep(100);
                    }

                    if (sessionIsDisconnected)
                    {
                        // No automatic reconnect attemps will be made by the API. It's up to the
                        // client application to reconnect the session
                        Console.WriteLine("About to connect the session ...");
                        if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                        {
                            Console.WriteLine("Session successfully connected");
                        }
                        else
                        {
                            Console.WriteLine("Failed to connect session, aborting ...");
                            return;
                        }
                        // The client application in this case is responsible for re-pulishing all unacked message
                        foreach (MessageRecord record in msgRecords)
                        {
                            if (!record.Acked)
                            {
                                ReturnCode rc = session.Send(record.Message);
                                if (rc != ReturnCode.SOLCLIENT_OK)
                                {
                                    Console.WriteLine("Failed to send unacked messages, aborting ...");
                                    return;
                                }
                            }
                        }
                    }
                    // Allocate a new message.
                    IMessage message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                    message.DeliveryMode = MessageDeliveryMode.Persistent;
                    // Create a record, and set it as CorrelationKey.
                    MessageRecord msgRecord = new MessageRecord(message);
                    message.CorrelationKey = msgRecord;
                    // Add it to the list of send message records and send it.
                    msgRecords.AddLast(msgRecord);
                    session.Send(message);

                    // Sleep for 500 msecs and check to see if the message was acknowledged (positively or negatively).
                    Thread.Sleep(500);
                    while (msgRecords.First != null && msgRecords.First.Value.Acked)
                    {
                        MessageRecord record = msgRecords.First.Value;
                        msgRecords.RemoveFirst();
                        Console.WriteLine(
                            string.Format("Freeing memory for message {0}, Result: Acked ({1}))\n",
                            i, record.Acked));
                        record.Message.Dispose();
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                // There should not be any left in the list, but just in case.
                foreach (MessageRecord record in msgRecords)
                {
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

