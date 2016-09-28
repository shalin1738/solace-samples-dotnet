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
//                        * RedirectLogs *
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
    public class RedirectLogs : SampleApp
    {
        // If set to true, the API logs will be printed to the error console.
        private bool PrintLogsToConsole = true;

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return @"Demonstrates redirecting API logs to the console";
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

        /// Prints an API log event to the error console.
        /// </summary>
        /// <param name="logInfo"></param>
        internal void LogItToErrorConsole(SolLogInfo logInfo)
        {
            Console.Error.WriteLine("- Redirected Log: " + logInfo.ToString());
        }

        /// <summary>
        /// Allow the RedirectLogs to initialize
        /// the API to redirect the logs to LogItToErrorConsole.
        /// </summary>
        public override void InitContext()
        {
            ContextFactoryProperties cfpProperties = new ContextFactoryProperties();
            if (PrintLogsToConsole)
            {
                // Print to error console delegate.
                cfpProperties.LogDelegate += LogItToErrorConsole;
            }
            // The logging level.
            cfpProperties.SolClientLogLevel = SolLogLevel.Debug;
            // Initialize the API.
            ContextFactory.Instance.Init(cfpProperties);
        }

        /// <summary>
        /// Main sample method
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
            cmdLineParser.Config.SetDestMode(DestMode.TOPIC);
            cmdLineParser.Config.DeliveryMode = MessageDeliveryMode.Direct;
            #endregion

            #region Initialize properties from command line.
            // Initialize the properties.
            ContextProperties contextProps = new ContextProperties();

            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Define IContext and ISession.
            IContext context = null;
            ISession session = null;
            IMessage message = null;
            try
            {
                InitContext();
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");
                Console.WriteLine("About to create the session ...");
                session = context.CreateSession(sessionProps,
                            SampleUtils.HandleMessageEvent,
                            SampleUtils.HandleSessionEvent);
                Console.WriteLine("Session successfully created.");

                Console.WriteLine("About to connect the session ...");
                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                    Console.WriteLine(GetRouterInfo(session));
                }
                message = SampleUtils.CreateMessage(cmdLineParser.Config, session);
                message.DeliveryMode = MessageDeliveryMode.Direct;
                message.Destination = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
                session.Send(message);
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
