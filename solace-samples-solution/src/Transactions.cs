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
//                        * Transactions *
//
// This sample demonstrates the following: 
// * Provisioning temporary queues,
// * Testing for transacted capability,
// * Creating transacted sessions,
// * Sending/receiving messages, and
// * Committing a transaction.
// 
#endregion
using System;
using System.Collections.Generic;
using SolaceSystems.Solclient.Messaging;
using System.Text;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    public class Transactions : SampleApp
    {
        #region Logging
        private static class Log
        {
            private static readonly object m_Lock = new object();
            private static int m_NextThreadNumber = 1;

            [ThreadStatic]
            private static int m_ThreadNumber;

            private static int ThreadNumber
            {
                get
                {
                    if (m_ThreadNumber == 0)
                    {
                        lock (m_Lock)
                        {
                            m_ThreadNumber = m_NextThreadNumber++;
                        }
                    }

                    return m_ThreadNumber;
                }
            }

            public static void Start(string msg)
            {
                Console.WriteLine("({0}) {1}... ", ThreadNumber, msg);
            }

            public static void Done()
            {
                Console.WriteLine("({0}) Done", ThreadNumber);
            }

            public static void Error() { Error(null); }
            public static void Error(string reason)
            {
                Console.WriteLine("({0}) Failed!", ThreadNumber);

                if (!string.IsNullOrEmpty(reason))
                {
                    Console.WriteLine("({0}) {1}", ThreadNumber, reason);
                }
            }


            public static void AssertOK(ReturnCode rc) { AssertOK(rc, null); }
            public static void AssertOK(ReturnCode rc, string reason)
            {
                if (rc != ReturnCode.SOLCLIENT_OK)
                {
                    Error(reason);
                }
                else
                {
                    Done();
                }
            }

            public static void AssertTrue(bool v) { AssertTrue(v, null); }
            public static void AssertTrue(bool v, string reason)
            {
                if (v)
                {
                    Done();
                }
                else
                {
                    Error(reason);
                }
            }

            public static void Print(string msg)
            {
                Console.WriteLine("({0}) {1}", ThreadNumber, msg);
            }
        }
        #endregion // Logging

        class TransactedSessionHolder : IDisposable
        {
            #region Fields
            protected readonly IContext m_Context;
            protected readonly ISession m_Session;
            protected readonly ITransactedSession m_TxSession;
            protected readonly IFlow m_Flow;
            protected readonly IQueue m_Queue;
            protected readonly string m_SenderId;
            #endregion

            #region Properties
            public IDestination ReceivingOn { get { return m_Queue; } }
            #endregion

            public TransactedSessionHolder(string senderId,
                                    ContextProperties contextProps,
                                    SessionProperties sessionProps)
            {
                m_SenderId = senderId;

                Log.Start("Creating the Context");
                m_Context = ContextFactory.Instance.CreateContext(contextProps, null);
                Log.Done();

                Log.Start("Creating the Session");
                m_Session = m_Context.CreateSession(sessionProps,
                                                    HandleSessionMessage,
                                                    HandleSessionEvent);
                Log.Done();

                Log.Start("Connecting the Session");
                Log.AssertOK(m_Session.Connect());

                Log.Start("Checking capabilities");
                Log.AssertTrue(m_Session.IsCapable(CapabilityType.TRANSACTED_SESSION),
                    "The 'TRANSACTED_SESSION' capability is required to run this sample");

                Log.Start("Creating Transacted Session");
                m_TxSession = m_Session.CreateTransactedSession(new TransactedSessionProperties());
                Log.Done();

                Log.Start("Creating Temporary Queue");
                m_Queue = m_Session.CreateTemporaryQueue();
                Log.Done();

                Log.Start("Creating consumer Flow");
                FlowProperties flowProps = new FlowProperties();
                flowProps.FlowStartState = true;
                EndpointProperties endpointProps = new EndpointProperties();
                m_Flow = m_TxSession.CreateFlow(flowProps,
                                                m_Queue,
                                                null,
                                                HandleTransactedMessage,
                                                HandleFlowEvent,
                                                endpointProps);
                Log.Done();
            }

            public void SendTo(IDestination dest)
            {
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.DeliveryMode = MessageDeliveryMode.Persistent;
                msg.SenderId = m_SenderId;
                msg.ReplyTo = m_Queue;
                msg.Destination = dest;

                Log.Start("Sending message");
                Log.AssertOK(m_TxSession.Send(msg));
            }

            public void Commit()
            {
                Log.Start("Committing");
                Log.AssertOK(m_TxSession.Commit());
            }

            void IDisposable.Dispose()
            {
                m_TxSession.Dispose();
                m_Queue.Dispose();
                m_Session.Dispose();
                m_Context.Dispose();
            }

            #region Event Handlers
            protected virtual void HandleSessionMessage(object sender, MessageEventArgs args) { }
            protected virtual void HandleSessionEvent(object sender, SessionEventArgs args) { }

            protected virtual void HandleTransactedMessage(object sender, MessageEventArgs args) { }
            protected virtual void HandleFlowEvent(object sender, FlowEventArgs args) { }
            #endregion
        }

        class Requestor : TransactedSessionHolder
        {
            private AutoResetEvent m_Barrier = new AutoResetEvent(false);

            public Requestor(ContextProperties contextProperties,
                                SessionProperties sessionProperties)
                : base("requestor", contextProperties, sessionProperties) { }

            public bool WaitForMessage(int timeout)
            {
                return m_Barrier.WaitOne(timeout);
            }

            protected override void HandleTransactedMessage(object sender, MessageEventArgs args)
            {
                m_Barrier.Set();
            }
        }

        class Responder : TransactedSessionHolder
        {
            public Responder(ContextProperties contextProperties,
                                SessionProperties sessionProperties)
                : base("responder", contextProperties, sessionProperties) { }

            protected override void HandleTransactedMessage(object sender, MessageEventArgs args)
            {
                Log.Print("Got a message!");
                this.SendTo(args.Message.ReplyTo);
                this.Commit();
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
            #endregion

            #region Initialize Properties
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            InitContext(cmdLineParser.LogLevel);

            using (Requestor requestor = new Requestor(contextProps, sessionProps))
            using (Responder responder = new Responder(contextProps, sessionProps))
            {
                requestor.SendTo(responder.ReceivingOn);
                requestor.Commit();

                Log.Start("Waiting for message");
                Log.AssertTrue(requestor.WaitForMessage(15000),
                    "Timeout while waiting for message");
            }
        }

        public override string ShortDescription()
        {
            return "Demonstrate transactions using a request/reply scenario";
        }

        public override bool GetIsUsingCommonArgs(out string extraOptionsFroCommonArgs, out string sampleSpecificUsge)
        {
            extraOptionsFroCommonArgs = null;
            sampleSpecificUsge = null;

            return true;
        }
    }
}
