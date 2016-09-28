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
//                        * SDTPubSubMsgDep *
//
// This sample demonstrates:
//  - Subscribing to a Topic.
//  - Publishing SDT map messages to the Topic.
//
// This sample creates a SDT stream and a SDT map directly in a
// Solace message. The stream is created as the binary attachment.
// The map is created as the user property map.
// The map is reused and modified for multiple sends.
// 
// Using message-dependent streams and maps avoids memory
// copy operations. It also makes the API responsible for managing 
// the memory allocated for the containers. However, 
// message-dependent streams and maps cannot be reused easily like 
// message-independent maps and streams.
//
#endregion
using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;
using SolaceSystems.Solclient.Messaging.SDT;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class SDTPubSubMsgDep : SampleApp
    {
        /// <summary>
        /// Short description
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Publisher and subscriber of message-dependent SDT messages";
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
        /// A Message receive callback delegate that prints the SDT content
        /// of received messages.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="args"></param>
        public void PrintReceivedMessage(Object source, MessageEventArgs args)
        {
            IMessage message = args.Message;
            ISDTContainer container = SDTUtils.GetContainer(message);
            StringBuilder sb = new StringBuilder();
            if (container is IMapContainer)
            {
                IMapContainer map = (IMapContainer)container;
                sb.Append("map={");
                while (map.HasNext())
                {
                    KeyValuePair<string, ISDTField> entry = map.GetNext();
                    sb.Append(string.Format("\n\tkey={0} value={1}", entry.Key, entry.Value.Value.ToString()));
                }
                sb.Append("}\n");
            }
            else if (container is IStreamContainer)
            {
                IStreamContainer stream = (IStreamContainer)container;
                sb.Append("stream={");
                while (stream.HasNext())
                {
                    ISDTField entry = stream.GetNext();
                    sb.Append(string.Format("\n\tvalue={0}", entry.Value.ToString()));
                }
                sb.Append("}\n");
            }
            SampleUtils.HandleMessageEvent(source, args);
            Console.WriteLine(sb.ToString());
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
            #endregion

            #region Initialize properties from command line
            // Initialize the properties
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext and ISession and ITopic
            IContext context = null;
            ISession session = null;
            ITopic topic = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps,
                    PrintReceivedMessage,
                    SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }

                topic = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);

                Console.WriteLine("About to subscribe to topic " + SampleUtils.SAMPLE_TOPIC);
                if (session.Subscribe(topic, true) == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Successfully added topic subscription");
                }

                // Create the message.
                IMessage message = ContextFactory.Instance.CreateMessage();

                // Create the message dependent stream.
                IStreamContainer stream = SDTUtils.CreateStream(message, 1024);

                // Populate the stream.
                stream.AddDouble(3.141592654);
                stream.AddString("message");

                // Create a user property map.
                IMapContainer userPropertyMap = message.CreateUserPropertyMap();

                // Add a well known integer to the user property map.
                userPropertyMap.AddInt32("mersenne", 43112609);
                
                // Set the message delivery options.
                message.DeliveryMode = MessageDeliveryMode.Direct;
                message.Destination = topic;

                int numMsgsToSend = 10;
                Console.WriteLine(string.Format("About to send {0} messages ...", numMsgsToSend));
                for (int i = 0; i < numMsgsToSend; i++)
                {
                    // Overwrite the "message" field and send.
                    userPropertyMap.DeleteField("message");
                    userPropertyMap.AddString("message", "message" + (i + 1));
                    session.Send(message);
                }

                // Dispose of the message.
                message.Dispose();

                Thread.Sleep(500); // wait for 0.5 seconds
                Console.WriteLine("\nDone");

            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            finally
            {
                if (session != null)
                {
                    if (topic != null)
                    {
                        session.Unsubscribe(topic, true);
                    }
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
