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
//                        * TopicDispatch *
//
// This sample demonstrates using local topic dispatch to direct received messages
// into specialized received data paths. It uses Session topic dispatch but it
// can be easily adapted for Flow topic dispatch.
//
// This sample performs the following steps:
// - Adds subscription "a/>" to the appliance
// - Adds local dispatch function 1 for topic "a/b"
// - Add dispatch function 2  and subscription for "c/>"
// - Add local dispatch function 3 for subscription "c/d"
// - publish on Topic a/c and verify receipt only on session callback
// - publish on Topic a/b and verify receipt only on dispatch function 1
// - publish on Topic c/d and verify receipt on both dispatch functions 2 and 3
// - publish on Topic c/e and verify receipt on only dispatch function 2
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class TopicDispatch : SampleApp
    {
        // Different dispatch targets used in this sample.
        IDispatchTarget dispatch_1 = null,
            dispatch_2 = null,
            dispatch_3 = null;
        // The Session to which the topic dispatch subscriptions are to be added.
        ISession session = null;

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Topic dispatch sample";
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

            // The different Topics used in this sample.
            ITopic ASlashWildCard = ContextFactory.Instance.CreateTopic("a/>");
            ITopic ASlashB = ContextFactory.Instance.CreateTopic("a/b");
            ITopic CSlashD = ContextFactory.Instance.CreateTopic("c/d");
            ITopic CSlashWildCard = ContextFactory.Instance.CreateTopic("c/>");
            ITopic ASlashC = ContextFactory.Instance.CreateTopic("a/c");
            ITopic CSlashE = ContextFactory.Instance.CreateTopic("c/e");

            // Parse Arguments and initialize session properties.
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            sessionProps.TopicDispatch = true; // This session property must be set to use topic dispatch capabilities

            try
            {
                InitContext(cmdLineParser.LogLevel);
                // Initialize the Context, connect the Session and assert capabilities.
                InitializeAndAssertCapabilities(ref context, ref this.session, sessionProps);

                // Add "a/>" Topic subscription to the Session.
                session.Subscribe(ASlashWildCard,true/*wait for confirm*/);

                // Create three dispatch targets.
                dispatch_1 = session.CreateDispatchTarget(ASlashB, HandleMessageEvent);
                session.Subscribe(dispatch_1, SubscribeFlag.LocalDispatchOnly, null); // local dispatch only

                dispatch_2 = session.CreateDispatchTarget(CSlashWildCard, HandleMessageEvent);
                session.Subscribe(dispatch_2, SubscribeFlag.WaitForConfirm, null); // subscribe to the appliance

                dispatch_3 = session.CreateDispatchTarget(CSlashD, HandleMessageEvent);
                session.Subscribe(dispatch_3, SubscribeFlag.LocalDispatchOnly, null); // local dispatch only

                // Publish to Topic a/c, and verify receipt only on the Session's message handler.
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.Destination = ASlashC;
                session.Send(msg);
                Console.WriteLine("\nPublished message to topic a/c");
                Thread.Sleep(100);

                // Publish to Topic a/b, and verify receipt only on dispatch_1 message handler.
                msg.Destination = ASlashB;
                session.Send(msg);
                Console.WriteLine("\nPublished message to topic a/b");
                Thread.Sleep(100);

                // Publish to Topic c/d, and verify receipt on both dispatch functions 2 and 3.
                msg.Destination = CSlashD;
                session.Send(msg);
                Console.WriteLine("\nPublished message to Topic c/d");
                Thread.Sleep(100);

                // Publish on Topic c/e, and verify receipt on only dispatch function 2.
                msg.Destination = CSlashE;
                session.Send(msg);
                Console.WriteLine("\nPublished message to Topic c/e");
                Thread.Sleep(100);
                
                // Wait for messages to be delivered.
                Thread.Sleep(Timeout);
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
                    session.Unsubscribe(ContextFactory.Instance.CreateTopic(">"), true/*wait for confirm*/);
                }
                if (dispatch_1 != null)
                {
                    session.Unsubscribe(dispatch_1, SubscribeFlag.LocalDispatchOnly, null);
                }
                if (dispatch_2 != null)
                {
                    session.Unsubscribe(dispatch_2, SubscribeFlag.WaitForConfirm, null);
                }
                if (dispatch_3 != null)
                {
                    session.Unsubscribe(dispatch_3, SubscribeFlag.LocalDispatchOnly, null);
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

        /// <summary>
        /// The delivered messages callback function.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        private void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            if (source == session)
            {
                Console.WriteLine(string.Format("\n\t(*) Default session delegate: received message with Topic = {0}", args.Message.Destination));
            }
            else
            {
                if (source is IDispatchTarget)
                {
                    Console.WriteLine(string.Format("\n\tDispatch delegate for '{0}': received message with Topic = {1}", 
                        ((IDispatchTarget)source).Subscription.ToString(), args.Message.Destination));
                }
            }
            args.Message.Dispose();
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
                        HandleMessageEvent,
                        SampleUtils.HandleSessionEvent);
            Console.WriteLine("Session successfully created.");
            Console.WriteLine("About to connect the Session ...");
            if (session.Connect() == ReturnCode.SOLCLIENT_OK)
            {
                Console.WriteLine("Session successfully connected");
                Console.WriteLine(GetRouterInfo(session));
            }
        }
    }
}
