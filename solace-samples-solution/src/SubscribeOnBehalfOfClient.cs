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
//                * SubscribeOnBehalfOfClient.cs *
// This sample shows how to subscribe on behalf of another client. Doing 
// so requires knowledge of the target client name, as well as possession of
// the subscription-manager permission.
//
// Two sessions are connected to the appliance, their ClientNames 
// are extracted, and session #1 adds a Topic subscription on 
// behalf of session #2. A message is then published on that Topic,
// which will be received by session #2.
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    /**
     * This sample shows how to subscribe on behalf of another client. Doing 
     * so requires knowledge of the target client name, as well as possession of
     * the subscription-manager permission.
     * 
     * Two sessions are connected to the appliance, their ClientNames 
     * are extracted, and session #1 adds a Topic subscription on 
     * behalf of session #2. A message is then published on that Topic,
     * which will be received by session #2.
     *
     * Copyright 2010-2016 Solace Systems, Inc. All rights reserved.
     */
    public class SubscribeOnBehalfOfClient : SampleApp
    {
        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Subscribe on behalf of another client.";
        }


        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = "Note: This sample requires that the client-username have the subscription-manager permission.\n";
            sampleSpecificUsage = null;
            return true;
        }

        private ISession session = null;
        private ISession session2 = null;
        private const string topic_str = "sample/topic/pasta";

        class IdPrintingReceiver {
            private readonly string _n;
            public IdPrintingReceiver(string name)
            {
                _n = name;
            }

            public void HandleMessageEvent(Object source, MessageEventArgs args)
            {
                Console.WriteLine("Received message on " + _n);
                SampleUtils.HandleMessageEvent(source, args);
            }
        }

        /// <summary>
        /// The main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args) {
            #region Parse Arguments
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // parse failed
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            #endregion

            #region Initialize properties from command line
            // Initialize the properties.
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext.
            IContext context = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                // We will create two sessions, and give them different msg receiver callbacks.
                IdPrintingReceiver recvCallback1 = new IdPrintingReceiver("Client 1");
                IdPrintingReceiver recvCallback2 = new IdPrintingReceiver("Client 2");

                Console.WriteLine("About to create the Sessions...");
                session = context.CreateSession(sessionProps,
                    recvCallback1.HandleMessageEvent, 
                    SampleUtils.HandleSessionEvent);
                session2 = context.CreateSession(sessionProps,
                    recvCallback2.HandleMessageEvent,
                    SampleUtils.HandleSessionEvent);
                Console.WriteLine("Sessions successfully created.");

                Console.WriteLine("About to connect the Session...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session 1 successfully connected.");
                    Console.WriteLine(GetRouterInfo(session));
                }

                Console.Write(String.Format("Check for capability: {0}... ", CapabilityType.SUBSCRIPTION_MANAGER));
                if (!session.IsCapable(CapabilityType.SUBSCRIPTION_MANAGER))
                {
                    Console.WriteLine("Not Supported. Exiting.");
                    return;
                } else {
                    Console.WriteLine("OK");
                }
                // Appliance supports this sample, connect 2nd session.
                if (session2.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session 2 successfully connected");
                }

                // Create the Topic to subscribe and send messages to.
                ITopic serviceTopic = ContextFactory.Instance.CreateTopic(topic_str);

                // Once clients have been connected, their ClientNames can be extracted.
                ContextFactory cf = ContextFactory.Instance;
                string strClientName1 = (string) session.GetProperty(SessionProperties.PROPERTY.ClientName);
                string strClientName2 = (string) session2.GetProperty(SessionProperties.PROPERTY.ClientName);
                IClientName clientName1 = cf.CreateClientName(strClientName1);
                IClientName clientName2 = cf.CreateClientName(strClientName2);

                Console.WriteLine("Client '{0}' adding subscription on behalf of client '{1}'", clientName1, clientName2);
                session.Subscribe(clientName2, serviceTopic, SubscribeFlag.RequestConfirm | SubscribeFlag.WaitForConfirm, null);
                Console.WriteLine("OK. Added subscription '{0}'.", serviceTopic);
                IMessage messageOne = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                messageOne.Destination = serviceTopic;

                Console.WriteLine("Sending a message from Client 1...");
                session.Send(messageOne);
                Console.WriteLine("Sent.");
                Thread.Sleep(500);
                Console.WriteLine("Done.");

            } catch (Exception ex) {
                PrintException(ex);
            }
            finally
            {
                if (session != null)
                {
                    session.Dispose();
                }
                if (session2 != null)
                {
                    session2.Dispose();
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
