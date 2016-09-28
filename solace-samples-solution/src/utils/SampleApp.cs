using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;

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
#endregion
namespace SolaceSystems.Solclient.Examples.Messaging
{
    public abstract class SampleApp : ISampleApp
    {


        protected int Timeout = 10000; // How long to wait before disconnecting.
        protected int NumberOfMessagesToPublish = 10; // The number of messages to send to the Queue.

        /// <summary>
        /// Initializes the API context
        /// </summary>
        /// 
        public virtual void InitContext()
        {
            InitContext(SolLogLevel.Warning);
        }

        /// <summary>
        /// Initializes the API context with a given log level
        /// </summary>
        /// <param name="logLevel"></param>
        public virtual void InitContext(SolLogLevel logLevel)
        {
            ContextFactoryProperties cfp = new ContextFactoryProperties();
            // Set log level.
            cfp.SolClientLogLevel = logLevel;
            // Log errors to console.
            cfp.LogToConsoleError();
            // Must init the API before using any of its artifacts.
            ContextFactory.Instance.Init(cfp);
            // Now we can print the version, after ContextFactory.Instance.Init()
            PrintVersion();

        }

        /// <summary>
        /// Cleans up the API context and related artifacts
        /// </summary>
        public virtual void CleanupContext()
        {
            ContextFactory.Instance.Cleanup();
        }

        /// <summary>
        /// Entry point to the sample
        /// </summary>
        /// <param name="args"></param>
        public void Call(string[] args)
        {
            SampleCall(args);
        }

        /// <summary>
        /// Prints the API version 
        /// </summary>
        public void PrintVersion()
        {
            IVersion version = ContextFactory.Instance.GetVersion();
            StringBuilder sb = new StringBuilder();
            sb.Append("\nVersion info:");
            sb.Append(string.Format("\n\t.NET Solclient {0} / {1} built on {2}", version.AssemblyFileVersion, version.AssemblyVersion,version.AssemblyBuildDate));
            sb.Append(string.Format("\n\t(Based on {0} / {1} built on {2})\n", version.NatvieSolClientVersion, version.NativeSolClientVariant,version.NativeSolClientBuildDate));
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Formats and prints to the console a given Exception instance
        /// </summary>
        /// <param name="ex"></param>
        public void PrintException(Exception ex)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("Encountered an exception:\n\tMessage = {0}\n", ex.Message));
            sb.Append(string.Format("\tType = {0}\n", ex.GetType().ToString()));
            sb.Append(string.Format("\tStack = {0}\n", ex.StackTrace));
            if (ex is OperationErrorException)
            {
                sb.Append(String.Format("\tOperation Error Info = {0}\n", ((OperationErrorException)ex).ErrorInfo.ToString()));
            }
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Demonstrates how to get router info and capabilities
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public string GetRouterInfo(ISession session)
        {
            CapabilityType[] capabilities =
             new CapabilityType[] {CapabilityType.BROWSER,CapabilityType.COMPRESSION,
                                   CapabilityType.ENDPOINT_MANAGEMENT,CapabilityType.ENDPOINT_MESSAGE_TTL,
                                   CapabilityType.MAX_DIRECT_MSG_SIZE, CapabilityType.MAX_GUARANTEED_MSG_SIZE,
                                   CapabilityType.MESSAGE_ELIDING, CapabilityType.PEER_PORT_SPEED,CapabilityType.PEER_PORT_TYPE,
                                   CapabilityType.PEER_SOFTWARE_DATE, CapabilityType.PUB_GUARANTEED, CapabilityType.QUEUE_SUBSCRIPTIONS,
                                   CapabilityType.SELECTOR, CapabilityType.SUB_FLOW_GUARANTEED, CapabilityType.SUBSCRIPTION_MANAGER,
                                   CapabilityType.SUPPORTS_XPE_SUBSCRIPTIONS, CapabilityType.TEMP_ENDPOINT};
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(string.Format("Appliance information: \n Appliance Name: {0}\n Platform: {1} \n Version: {2} \n ",
                    session.GetCapability(CapabilityType.PEER_ROUTER_NAME).Value.Value,
                    session.GetCapability(CapabilityType.PEER_PLATFORM).Value.Value,
                    session.GetCapability(CapabilityType.PEER_SOFTWARE_VERSION).Value.Value));
                sb.Append("Appliance Capabilities:\n");
                for (int i = 0 ; i < capabilities.Length ; i++) 
                {
                    sb.Append(string.Format("\t{0} : {1}\n", capabilities[i],session.GetCapability(capabilities[i]).Value.Value));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return "Error occurred when getting appliance capabilities, reason: " + ex.Message;
            }
        }

        /// <summary>
        /// Prints the sample's usage (command line argument accepted by the sample)
        /// </summary>
        /// <param name="err"></param>
        protected void PrintUsage(string err)
        {
            string extraOptionsForCommonArgs = null;
            string sampleSpecificUsage = null;
            bool usesCommonArgs = GetIsUsingCommonArgs(out extraOptionsForCommonArgs, out sampleSpecificUsage);
            string usageStr = "";
            if (usesCommonArgs)
            {
                usageStr = ArgParser.CommonUsage;
                if (extraOptionsForCommonArgs != null)
                {
                    usageStr += "Extra arguments or notes for this sample:\n" + extraOptionsForCommonArgs;
                }
            } else {
                usageStr = sampleSpecificUsage;
            }
            if (err != null && !err.Trim().Equals(""))
            {
                usageStr = "Exception: " + err + "\n\n" + usageStr;
            }
            Console.WriteLine(usageStr);
        }
        protected readonly static string INVALID_ARGUMENTS_ERROR = "Invalid command line arguments";

        /// <summary>
        /// Inherited from ISampleApp
        /// </summary>
        /// <param name="args"></param>
        public abstract void SampleCall(string[] args);

        /// <summary>
        /// Initializes the API context
        /// </summary>
        /// <returns></returns>
        public abstract string ShortDescription();

        /// <summary>
        /// Initializes the API context
        /// </summary>
        /// <param name="extraOptionsForCommonArgs"></param>
        /// <param name="sampleSpecificUsage"></param>
        /// <returns></returns>
        public abstract bool GetIsUsingCommonArgs(out string extraOptionsForCommonArgs, out string sampleSpecificUsage);

    }
}
