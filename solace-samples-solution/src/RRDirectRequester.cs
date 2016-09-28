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
//                        * RRDirectRequester *
// This sample demonstrates a client sending a request to a replier or service 
// using the Request/Reply message exchange pattern with direct messaging. 
// The service is represented by RRDirectReplier which computes arithmetic expressions of the
// form <left operand> <operation> <right operand> (e.g. 3 + 9 = 12)
// The RRDirectReplier has to be running to get a response message.
// 
// This sample will perform the following:
//   Connect a session to the appliance and send a request message to the request topic,
//   passed in by the user as a command line argument. The message will have in its payload
//   a SDT stream with the following fields:
//   (1) int8: <operation> a byte that represents the arithmetic operation to be performed by the service
//             or RRDirectReplier
//   (2) int32: <left operand> 
//   (3) int32: <right operand>
// 
// Note: see RRDirectReplier.cs for more details
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;
using SolaceSystems.Solclient.Messaging.SDT;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    class RRDirectRequester : SampleApp
    {
        // The topic to send the request message to
        // It represents the topic string which the RRDirectReplier 
        // is listening on for requests
        private string requestTopicStr = null;

        ISession session = null;

        private void doRequest(Operation operation, int leftOperand, int rightOperand)
        {
            // Create the request message
            IMessage requestMessage = ContextFactory.Instance.CreateMessage();
            requestMessage.Destination = ContextFactory.Instance.CreateTopic(requestTopicStr);
            requestMessage.DeliveryMode = MessageDeliveryMode.Direct; /* explicitly set to MessageDeliveryMode.Direct */
            IStreamContainer stream = SDTUtils.CreateStream(requestMessage, 256);
            stream.AddInt8((short)operation);
            stream.AddInt32(leftOperand);
            stream.AddInt32(rightOperand);

            // Send the request message to the service or RRDirectReplier
            IMessage replyMessage = null;
            int timeout = 2000; /* 2 secs*/
            Console.WriteLine("\nSending  request message, waiting for {0} msecs for a reply (make sure that RRDirectReply is running) ...", timeout);
            Console.WriteLine(ARITHMETIC_EXPRESSION,
                leftOperand, operation.ToString(), rightOperand, "?");
            if (session.SendRequest(requestMessage, out replyMessage, 2000) == ReturnCode.SOLCLIENT_OK)
            {
                // Got a reply, format and print the response message
                Console.WriteLine("\nGot reply message");
                IStreamContainer respStream = (IStreamContainer)SDTUtils.GetContainer(replyMessage);
                if (respStream != null)
                {
                    ISDTField status = respStream.GetNext();
                    if (status.Type == SDTFieldType.BOOL)
                    {
                        if (((bool)status.Value))
                        {
                            Console.WriteLine(ARITHMETIC_EXPRESSION,
                                            leftOperand, operation.ToString(), rightOperand, respStream.GetNext().Value.ToString());
                        }
                        else
                        {
                            Console.WriteLine(ARITHMETIC_EXPRESSION,
                                            leftOperand, operation.ToString(), rightOperand, "operation failed");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Request failed");
            }
            if (requestMessage != null)
            {
                // It is a good practice to dispose of messages once done using them
                requestMessage.Dispose();
            }
        }

        /// <summary>
        /// Main entry point to the sample
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            // Parse command line arguments
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args) || !SampleParseArgs(cmdLineParser)) 
            {
                // Parse failed.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }

            // Create the API components: starting with the properties 
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = new SessionProperties();
            sessionProps.Host = cmdLineParser.Config.IpPort.ip;
            sessionProps.UserName = cmdLineParser.Config.RouterUserVpn.user;
            sessionProps.Password = cmdLineParser.Config.UserPassword;
            sessionProps.SSLValidateCertificate = false; 
            sessionProps.ReconnectRetries = 3;
            if (cmdLineParser.Config.RouterUserVpn.vpn != null)
            {
                sessionProps.VPNName = cmdLineParser.Config.RouterUserVpn.vpn;
            }
            if (cmdLineParser.Config.Compression)
            {
                /* Compression is set as a number from 0-9, where 0 means "disable
                   compression", and 9 means max compression. The default is no
                   compression.
                   Selecting a non-zero compression level auto-selects the
                   compressed SMF port on the appliance, as long as no SMF port is
                   explicitly specified. */
                sessionProps.CompressionLevel = 9;
            }

            // Create and connect the API components: create the context and session objects
            IContext context = null;
            
            try
            {
                // Creating the context
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("Creating the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);

                // Creating the session
                Console.WriteLine("Creating the session ...");
                session = context.CreateSession(sessionProps, SampleUtils.HandleMessageEvent, SampleUtils.HandleSessionEvent);

                // Connecting the session
                Console.WriteLine("Connecting the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                }

                doRequest(Operation.PLUS, 5, 4);
                Thread.Sleep(1000);
                doRequest(Operation.MINUS, 5, 4);
                Thread.Sleep(1000);
                doRequest(Operation.TIMES, 5, 4);
                Thread.Sleep(1000);
                doRequest(Operation.DIVIDE, 5, 4);
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
                if (context != null)
                {
                    context.Dispose();
                }
                // Must cleanup after. 
                CleanupContext();
            }
        }

        /// <summary>
        /// Operations a client can use when talking to the RRDirectReplier.
        /// </summary>
        private enum Operation
        {
            PLUS=1, MINUS=2, TIMES=3, DIVIDE=4
        }

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "An API sample that demonstrates a requester using direct messaging";
        }

        /// <summary>
        /// Command line arguments options
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public override bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage)
        {
            extraOptionsForCommonArgs = "\t -rt  \t the topic to send the request message to (RRDirectReplier should be listeneing on the same topic)";
            sampleSpecificUsage = null;
            return true;
        }

        /// <summary>
        /// Parse the sample's extra command line arguments.
        /// </summary>
        /// <param name="args"></param>
        private bool SampleParseArgs(ArgParser cmdLineParser)
        {
            string requestTopicArgOption = "-rt";
            try
            {
                requestTopicStr = cmdLineParser.Config.ArgBag[requestTopicArgOption];
            }
            catch (KeyNotFoundException)
            {
                Console.WriteLine(String.Format("Invalid command line argument '{0}' ", requestTopicStr));
                return false;
            }
            return true;
        }

        // format for the arithmetic operation
        private readonly string ARITHMETIC_EXPRESSION = "\t=================================\n\t  {0} {1} {2} = {3}  \t\n\t=================================\n";
    }
}
