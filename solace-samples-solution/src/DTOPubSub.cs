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
//                    * DTOPubSub *
// This sample demonstrates:
//  1. Publishing a message using Deliver-To-One (DTO)
//  2. Subscribing to a Topic using DTO override to receive all messages.
//
// In this sample, three sessions to a appliance running SolOS-TR are created:
//  session     - Publish messages to the topic with the DTO flag set.
//              - Subscribe to the topic with DTO override set.
//  dtoSession1 - Subscribe to the topic.
//  dtoSession2 - Subscribe to the topic.
//
// With the DTO flag set on messages being published, the appliance will deliver
// messages to 'dtoSession1' and 'dtoSession2' in a round robin manner. In 
// addition  to delivering the message to either 'dtoSession1' or 
// 'dtoSession2', the appliance will deliver all messages to 'session'.  
//
// Note: 'session' is not part of the round robin to receive DTO messages
// because its subscription uses DTO-override.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class DTOPubSub : SampleApp
    {
        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "DTO sample using publishers and subscribers";
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
        /// Message handler class that takes a session name as a parameter
        /// to the constructor and outputs this session name when a message
        /// is received.
        /// </summary>
	    public class DtoPrintingMessageHandler 
        {
		    public DtoPrintingMessageHandler(String sessionNameArg)
            {
			    this.sessionName = sessionNameArg;
		    }

            public void onReceive(Object source, MessageEventArgs args)
            {
                Console.WriteLine("{0} received message. (seq# {1})", sessionName, args.Message.SequenceNumber);
                SampleUtils.HandleMessageEvent(source, args);
            }

            private String sessionName;
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
            cmdLineParser.Config.DeliveryMode = MessageDeliveryMode.Direct;
            #endregion

            #region Initialize properties from command line
            // Initialize the properties.
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext and three ISessions.
            IContext context = null;
            ISession session = null;
            ISession dtoSession1 = null;
            ISession dtoSession2 = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);

                #region Create the Context
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");
                #endregion

                #region Create and Connect 3 sessions
                Console.WriteLine("About to create the sessions ...");

                // To demontrate where the messages are being received, a 
                // custom message handler class is used. This message handler
                // enables a session name to be specified. The name is included in
                // the ouptut when a message is received.
                DtoPrintingMessageHandler sessionMessageHandler1 = new DtoPrintingMessageHandler("DTO Override Session");
                DtoPrintingMessageHandler sessionMessageHandler2 = new DtoPrintingMessageHandler("DTO Session 1");
                DtoPrintingMessageHandler sessionMessageHandler3 = new DtoPrintingMessageHandler("DTO Session 2");

                session = context.CreateSession(sessionProps, sessionMessageHandler1.onReceive, SampleUtils.HandleSessionEvent);
                dtoSession1 = context.CreateSession(sessionProps, sessionMessageHandler2.onReceive, SampleUtils.HandleSessionEvent);
                dtoSession2 = context.CreateSession(sessionProps, sessionMessageHandler3.onReceive, SampleUtils.HandleSessionEvent);

                Console.WriteLine("Sessions successfully created.");

                Console.WriteLine("About to connect the sessions ...");
                if ( (session.Connect() == ReturnCode.SOLCLIENT_OK) &&
                     (dtoSession1.Connect() == ReturnCode.SOLCLIENT_OK) &&
                     (dtoSession2.Connect() == ReturnCode.SOLCLIENT_OK) )
                {
                    Console.WriteLine("Sessions successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }
                #endregion

                #region Define and subscribe to topics on all 3 sessions
                // All clients subscribe to the same Topic.
                TopicProperties topicProps = new TopicProperties();
                topicProps.Topic = SampleUtils.SAMPLE_TOPIC;

                // Create a Topic subscription with Deliver Always enabled.
                topicProps.IsReceiveAllDeliverToOne = true;
                ITopic myTopic_deliverAlways = ContextFactory.Instance.CreateTopic(topicProps);

                // Create a Topic subscription with Deliver Always disabled.
                topicProps.IsReceiveAllDeliverToOne = false;
                ITopic myTopic = ContextFactory.Instance.CreateTopic(topicProps);

                // Add the subscriptions.
                bool waitForConfirm = true;
                session.Subscribe(myTopic_deliverAlways, waitForConfirm);
                dtoSession1.Subscribe(myTopic, waitForConfirm);
                dtoSession2.Subscribe(myTopic, waitForConfirm);
                #endregion

                // Create an empty message to send to our topic. The message
                // can be empty because we only care about where it gets
                // delivered and not what the contents are.
                IMessage msg = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                msg.Destination = myTopic;
                msg.DeliverToOne = true;

                Console.WriteLine(string.Format("About to send {0} messages ...", NumberOfMessagesToPublish));
                for (int i = 0; i < NumberOfMessagesToPublish; i++)
                {
                    // This call accesses custom header data and could impact performance.
                    msg.SequenceNumber = i + 1;

                    if (session.Send(msg) == ReturnCode.SOLCLIENT_OK)
                    {
                        Console.WriteLine("Message {0} sent.", i + 1);
                    }
                    Thread.Sleep(500); // wait for 0.5 seconds
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
                if (session != null)
                {
                    session.Dispose();
                }
                if (dtoSession1 != null)
                {
                    dtoSession1.Dispose();
                }
                if (dtoSession2 != null)
                {
                    dtoSession2.Dispose();
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
