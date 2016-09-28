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
//                        * QueueProvisionAndBrowse *
// This sample demonstrates how to provision an endpoint (in this case a 
// Queue) on the appliance, then how to use a Browser to browse its contents. 
// Every message received through the Browser interface is dumped to screen 
// using the Dump convenience function for review.
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
    public class QueueProvisionAndBrowse : SampleApp
    {

        /// <summary>
        /// Short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Queue provision and browse";
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

            // Define IContext and ISession.
            IContext context = null;
            ISession session = null;
            IBrowser browser = null;
            IQueue queue = null;
            try
            {
                InitContext(cmdLineParser.LogLevel);
                #region Initialize the context, connect the Session, and assert capabilities.
                Console.WriteLine("About to create the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);
                Console.WriteLine("Context successfully created ");
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
                // Does the appliance support these capabilities:PUB_GUARANTEED,ENDPOINT_MANAGEMENT,BROWSER?
                Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.PUB_GUARANTEED));
                if (!session.IsCapable(CapabilityType.PUB_GUARANTEED))
                {
                    Console.WriteLine("Not Supported\n Exiting");
                    return;
                }
                else
                {
                    Console.WriteLine("Supported");
                }
                Console.Write(String.Format("Check for capability: {0} ... ",CapabilityType.ENDPOINT_MANAGEMENT));
                if (!session.IsCapable(CapabilityType.ENDPOINT_MANAGEMENT))
                {
                    Console.WriteLine("Not Supported\n Exiting");
                    return;
                }
                else
                {
                    Console.WriteLine("Supported");
                }
                Console.Write(String.Format("Check for capability: {0} ... ", CapabilityType.BROWSER));
                if (!session.IsCapable(CapabilityType.BROWSER))
                {
                    Console.WriteLine("Not Supported \n Exiting");
                    return;
                }
                else
                {
                    Console.WriteLine("Supported");
                }
                #endregion

                #region Provision a new queue endpoint
                EndpointProperties endpointProps = new EndpointProperties();
                // Set permissions to allow all permissions to others.
                endpointProps.Permission = EndpointProperties.EndpointPermission.Delete;
                // Set access type to exclusive. 
                endpointProps.AccessType = EndpointProperties.EndpointAccessType.Exclusive;
                // Set quota to 100 MB.
                endpointProps.Quota = 100;
                string queueName = "solclient_dotnet_sample_QueueProvisionAndBrowse_" + (new Random()).Next(1000);


                queue = ContextFactory.Instance.CreateQueue(queueName);
                Console.WriteLine(String.Format("About to provision queue '{0}' on the appliance", queueName));
                try
                {
                    session.Provision(queue /* endpoint */, 
                        endpointProps /*endpoint properties */, 
                        ProvisionFlag.WaitForConfirm /* block waiting for confirmation */, 
                        null /*no correlation key*/);
                    Console.WriteLine("Endpoint queue successfully provisioned on the appliance");
                }
                catch (Exception ex)
                {
                    PrintException(ex);
                    Console.WriteLine("Exiting");
                    return;
                }
                #endregion

                #region Publishing some messages to this newly provisioned queue
                IMessage msg = ContextFactory.Instance.CreateMessage();
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                msg.DeliveryMode = MessageDeliveryMode.Persistent;
                msg.Destination = queue;
                int howManyToPublish = 10;
                Console.WriteLine(String.Format("About to publish {0} messages",howManyToPublish));
                for (int i = 0; i < howManyToPublish; i++)
                {
                    if (session.Send(msg) == ReturnCode.SOLCLIENT_OK)
                    {
                        Console.Write(".");
                    }
                    Thread.Sleep(100); // wait for 0.5 seconds
                }
                Console.WriteLine("\nDone");
                #endregion

                #region Create a Browser to the newly provisioned Queue, and selectively remove the spooled messages.
                BrowserProperties browserProps = new BrowserProperties();
                browser = session.CreateBrowser(queue, browserProps);
                Console.WriteLine(string.Format("Browsing queue {0} ", queueName));
                int count = 0, removedCount = 0;
                while ((msg = browser.GetNext()) != null)
                {
                    Console.WriteLine(msg.Dump());
                    count++;
                    if (count % 3 == 0)
                    {
                        // A message may be removed by calling IBrowser.remove(). 
                        // This deletes it from the appliance's message spool.
                        Console.WriteLine("Removing spooled message ({0}).", count);
                        browser.Remove(msg);
                        removedCount++;
                    }
                }
                Console.WriteLine(string.Format("Browsed {0} messages, removed {1} ", count, removedCount));
                #endregion

            }
            catch (Exception ex)
            {
                PrintException(ex);
                Console.WriteLine("Exiting");
            }
            finally
            {
                if (browser != null)
                {
                    browser.Dispose();
                }
                if (session != null)
                {
                    session.Deprovision(queue, ProvisionFlag.WaitForConfirm, null);
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
