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
//                * SyncCacheRequest *
//  This sample demonstrates:
//  - Creating a cache session.
//  - Sending a synchronous cache request.
//
// Sample Requirements:
//  - A Solace appliance running SolOS-TR with an active cache.
//  - A cache running and caching on a pattern that matches "my/sample/topic".
//  - The cache name must be known and passed to this program as a command line
//    argument. 
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using SolaceSystems.Solclient.Messaging.Cache;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class SyncCacheRequest : SampleApp
    {

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Synchronous cache request sample";
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
        /// The main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            #region Parse Arguments
            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.ParseCacheSampleArgs(args))
            {
                // Parse failed.
                Console.WriteLine("Exception: " + INVALID_ARGUMENTS_ERROR);
                Console.Write(ArgParser.CacheArgUsage);
                return;
            }
            #endregion

            #region Initialize properties from command line
            // Initialize the properties.
            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            #endregion

            // Context and session.
            IContext context = null;
            ISession session = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created. ");

                Console.WriteLine("About to create the Session ...");
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
                #region PUBLISH A MESSAGE (just to make sure there is one cached)

                ITopic topic = null;
                topic = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
                IMessage message = ContextFactory.Instance.CreateMessage();
                message.Destination = topic;
                message.DeliveryMode = MessageDeliveryMode.Direct;
                message.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                session.Send(message);
                #endregion

                CacheSessionConfiguration cacheConfig = (CacheSessionConfiguration) cmdLineParser.Config;
                ICacheSession cacheSession = SampleUtils.newCacheSession(session, cacheConfig);
                ReturnCode rc = cacheSession.SendCacheRequest(1, topic, cacheConfig.Subscribe, cacheConfig.Action);
                Console.WriteLine("Cache Response: " + rc.ToString());
                IDictionary<Stats_Rx, Int64> stats = session.GetRxStats();
                SampleUtils.PrintRxStats(stats);
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
