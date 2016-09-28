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

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Examples.Messaging.Samples;
using System.Collections;

namespace SolaceSystems.Solclient.Examples.Messaging
{
    class SolClientSamples
    {
        public class SampleAppComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                return x.ToString().CompareTo(y.ToString());
            }
        }

        public enum SampleApp
        {
            DirectPubSub,
            SimpleFlowToQueue,
            SimpleFlowToTopic,
            DTOPubSub,
            SDTPubSubMsgDep,
            SDTPubSubMsgIndep,
            SempPagingRequests,
            SempHttpSetRequest,
            SempGetOverMB,
            SyncCacheRequest,
            AsyncCacheRequest,
            QueueProvisionAndBrowse,
            MessageSelectorsOnQueue,
            MessageTTLAndDeadMessageQueue,
            TopicToQueueMapping,
            TopicDispatch,
            SubscribeOnBehalfOfClient,
            EventMonitor,
            AdPubAck,
            RedirectLogs,
            Replication,
            NoLocalPubSub,
            ActiveFlowIndication,
            SecureSession,
            RRDirectRequester,
            RRDirectReplier,
            RRGuaranteedRequester,
            RRGuaranteedReplier,
            Transactions,
        }



        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                printUsage();
                return;
            }

            try
            {
                string s_name = args[0];
                SampleApp s_app = (SampleApp)Enum.Parse(typeof(SampleApp), s_name, true);
                ISampleApp app = instantiateSample(s_app);
                string[] app_args = new string[args.Length-1];
                Array.Copy(args, 1, app_args, 0, app_args.Length);
                Console.WriteLine(
                    "\nLaunching '{0}': {1} ...", 
                    Enum.GetName(typeof(SampleApp), s_app), 
                    app.ShortDescription());
                app.Call(app_args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("---\n" + ex + "\n---");
                printUsage();
                return;
            }
        }

        static ISampleApp instantiateSample(SampleApp s)
        {
            switch (s)
            {
                case SampleApp.ActiveFlowIndication:
                    return new ActiveFlowIndication();
                case SampleApp.DirectPubSub:
                    return new DirectPubSub();
                case SampleApp.SimpleFlowToQueue:
                    return new SimpleFlowToQueue();
                case SampleApp.SimpleFlowToTopic:
                    return new SimpleFlowToTopic();
                case SampleApp.DTOPubSub:
                    return new DTOPubSub();
                case SampleApp.SDTPubSubMsgDep:
                    return new SDTPubSubMsgDep();
                case SampleApp.SDTPubSubMsgIndep:
                    return new SDTPubSubMsgIndep();
                case SampleApp.SempPagingRequests:
                    return new SempPagingRequests();
                case SampleApp.SempHttpSetRequest:
                    return new SempHttpSetRequest();
                case SampleApp.SyncCacheRequest:
                    return new SyncCacheRequest();
                case SampleApp.AsyncCacheRequest:
                    return new AsyncCacheRequest();
                case SampleApp.QueueProvisionAndBrowse:
                    return new QueueProvisionAndBrowse();
                case SampleApp.MessageSelectorsOnQueue:
                    return new MessageSelectorsOnQueue();
                case SampleApp.MessageTTLAndDeadMessageQueue:
                    return new MessageTTLAndDeadMessageQueue();
                case SampleApp.TopicToQueueMapping:
                    return new TopicToQueueMapping();
                case SampleApp.TopicDispatch:
                    return new TopicDispatch();
                case SampleApp.SubscribeOnBehalfOfClient:
                    return new SubscribeOnBehalfOfClient();
                case SampleApp.EventMonitor:
                    return new EventMonitor();
                case SampleApp.AdPubAck:
                    return new AdPubAck();
                case SampleApp.RedirectLogs:
                    return new RedirectLogs();
                case SampleApp.Replication:
                    return new Replication();
                case SampleApp.SempGetOverMB:
                    return new SempGetOverMB();
                case SampleApp.NoLocalPubSub:
                    return new NoLocalPubSub();
                case SampleApp.SecureSession:
                    return new SecureSession();
                case SampleApp.RRDirectRequester:
                    return new RRDirectRequester();
                case SampleApp.RRDirectReplier:
                    return new RRDirectReplier();
                case SampleApp.RRGuaranteedRequester:
                    return new RRGuaranteedRequester();
                case SampleApp.RRGuaranteedReplier:
                    return new RRGuaranteedReplier();
                case SampleApp.Transactions:
                    return new Transactions();
                default:
                    return null;
            }
        }

        static void printUsage()
        {
            Console.WriteLine("\nSolClient Sample Application Launcher, Copyright 2008-2016 Solace Systems, Inc.");
            Console.WriteLine("Usage: SolClientSamples.exe <SAMPLENAME> <SAMPLE ARGS...> ");
            Console.WriteLine("\nWhere <SAMPLENAME> is one of the following:");
            SampleApp[] sampleAppEnums = (SampleApp[])Enum.GetValues(typeof(SampleApp));
            Array.Sort(sampleAppEnums,new SampleAppComparer());
            foreach(SampleApp s in sampleAppEnums)
            {
                string s_name = Enum.GetName(typeof(SampleApp), s);
                string msg = s_name.PadRight(28);
                ISampleApp app = instantiateSample(s);
                if (app != null)
                {
                    msg += app.ShortDescription();
                }
                Console.WriteLine(msg);
            }

            Console.WriteLine("\n<SAMPLE ARGS...> are arguments to pass to this sample application.");
            Console.WriteLine("Specify no arguments to get usage information for the selected sample.\n");
        }
    }
}
