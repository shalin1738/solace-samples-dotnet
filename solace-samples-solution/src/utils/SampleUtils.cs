using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.SDT;
using SolaceSystems.Solclient.Messaging.Cache;

#region Copyright & License
// Utility class supporting the .NET API sample applications.
// Provides common functionality like command-line argument 
// parsing and logging methods.
//
// Copyright 2008-2016 Solace Systems Inc. All rights reserved.
// http://www.SolaceSystems.com
// 
#endregion
namespace SolaceSystems.Solclient.Examples.Messaging
{
    public class IpPort
    {
        public string ip;

        public IpPort(string ip)
        {
            this.ip = ip;
        }

        internal static IpPort Parse(string ipport)
        {
            if (ipport == null || ipport.Trim().Equals(""))
            {
                throw new ArgumentOutOfRangeException("Unable to parse empty ip:port");
            }
            return new IpPort(ipport);
        }
    }

    public class UserVpn
    {
        public string user;
        public string vpn;

        public UserVpn(string user, string vpn)
        {
            this.user = user;
            this.vpn = vpn;
        }
        internal static UserVpn Parse(string uservpn)
        {
		    string[] parts = uservpn.Split('@');
			switch (parts.Length) {
			case 1:
				return new UserVpn(parts[0], null);
			case 2:
				return new UserVpn(parts[0], parts[1]);
			}
            throw new ArgumentOutOfRangeException("Unable to parse " + uservpn);
        }
    }
    
    class SampleUtils
    {
        public static readonly string SAMPLE_TOPIC = "my/sample/topic";
        public static readonly string SAMPLE_TOPIC_DELIVER_ALWAYS = "my/sample/topic/da";
        public static readonly string SAMPLE_QUEUE = "my_sample_queue";
        public static readonly string SAMPLE_TOPICENDPOINT = "my_sample_topicendpoint";
        public static readonly string SAMPLE_XPE = "/sample";

        public static readonly string MSG_XMLDOC = "<sample>1</sample>";
        public static readonly string MSG_XMLDOCMETA = "<sample><metadata>1</metadata></sample>";
        public static readonly string MSG_ATTACHMENTTEXT = "my attached data";

        /// <summary>
        /// Creates a SessionProperties instance based on parsed SessionConfiguration
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        public static SessionProperties NewSessionPropertiesFromConfig(SessionConfiguration sc)
        {
            SessionProperties sessionProps = new SessionProperties();
            // Message backbone IP
            sessionProps.Host = sc.IpPort.ip;
            // User name
            if (sc.RouterUserVpn != null)
            {
                sessionProps.UserName = sc.RouterUserVpn.user;
                if (sc.RouterUserVpn.vpn != null)
                {
                    sessionProps.VPNName = sc.RouterUserVpn.vpn;  // applies to SolOS-TR only
                }
            }
            // Turn ReapplySubscriptions on to enable api-based subscription management
            sessionProps.ReapplySubscriptions = true;

            sessionProps.Password = sc.UserPassword;
            if (sc.Compression)
            {
                //
                // Compression is set as a number from 0-9. 0 means
                // "disable compression" (the default), and 9 means maximum compression.
                // Selecting a non-zero compression level auto-selects the
                // compressed SMF port on the appliance, as long as no SMF port is
                // explicitly specified.
                
                sessionProps.CompressionLevel = 9;
            }
            // To enable session reconnect
            sessionProps.ReconnectRetries = 100; // retry 100 times
            sessionProps.ReconnectRetriesWaitInMsecs = 3000; // 3 seconds

            //SSL properties.
            sessionProps.SSLTrustStoreDir = sc.trustStoreDirectory;
            sessionProps.SSLTrustedCommonNameList = sc.commonNames;
            sessionProps.SSLCipherSuites = sc.cipherSuites;
            sessionProps.SSLExcludedProtocols = sc.excludedProtocols;
            sessionProps.SSLValidateCertificate = sc.validateCertificate;
            sessionProps.SSLValidateCertificateDate = sc.validateCertificateDate;
            sessionProps.AuthenticationScheme = sc.authenticationScheme;
            sessionProps.SSLClientPrivateKeyFile = sc.clientPrivateKeyFile;
            sessionProps.SSLClientPrivateKeyFilePassword = sc.clientPrivateKeyPassword;
            sessionProps.SSLClientCertificateFile = sc.clientCertificateFile;
            sessionProps.SSLConnectionDowngradeTo = sc.sslConnectionDowngradeTo;

            // Uncomment the following statement to enable automatic timestamp generation on sent messages.
            // When enabled, a send timestamp is automatically generated as a message property for each message sent.  
            // This adds a binary meta part to the message which can reduce performance.
            // sessionProps.GenerateSendTimestamps = true;

            // Uncomment the following statement to enable automatic sequence number generation on sent messages.
            // When enabled, a sequence number is automatically included in the Solace-defined fields for each message sent.   
            // This adds a binary meta part to the message which can reduce performance.
            // sessionProps.GenerateSequenceNumber = true;

            // Uncomment the following statement to enable the inclusion of senderId on sent messages.
            // When enabled, a sender ID is automatically included in the Solace-defined fields for each message sent.    
            // This adds a binary meta part to the message which can reduce performance.
            // sessionProps.IncludeSenderId = true;

            return sessionProps;
        }


        /// <summary>
        /// Creates an ICacheSession given an ISession and CacheSessionConfiguration
        /// </summary>
        /// <param name="jcsmpSession"></param>
        /// <param name="sc"></param>
        /// <returns></returns>
        public static ICacheSession newCacheSession(ISession session, CacheSessionConfiguration sc)
        {
            CacheSessionProperties cacheProps = new CacheSessionProperties();
            cacheProps.CacheName = sc.CacheName;
            cacheProps.MaxMessagesPerTopic = sc.MaxMsgs;
            cacheProps.MaxMessageAgeInSecs = sc.MaxAge;
            cacheProps.CacheRequestTimeoutInMsecs = sc.Timeout;
            return session.CreateCacheSession(cacheProps);
        }

        /// <summary>
        /// Parses arguments into key/value pairs. The args array should contain key/value pairs like this:
        /// -u asdf -p foo --type direct
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseArgDictionary(string[] args)
        {
            Dictionary<string, string> parsed = new Dictionary<string, string>();
            for (int i = 0; i < args.Length; i++)
            {
                string s = args[i], arg_name = "";
                if (s.StartsWith("--"))
                    arg_name = s.Substring(2);
                else if (s.StartsWith("-"))
                    arg_name = s.Substring(1);
                else
                    throw new Exception("Illegal argument " + s);

                parsed.Add(arg_name.ToLower(), args[++i]);
            }
            return parsed;
        }


        /// <summary>
        /// Parses IpPort from string
        /// </summary>
        /// <param name="ip_port"></param>
        /// <returns></returns>
        public static IpPort ParseIpPort(string ip_port)
        {
            return new IpPort(ip_port);
        }

        /// <summary>
        /// Parses UserVpn structure from string
        /// </summary>
        /// <param name="user_vpn"></param>
        /// <param name="defaultVpn"></param>
        /// <returns></returns>
        public static UserVpn ParseUserVpn(string user_vpn, string defaultVpn)
        {
            UserVpn ret = new UserVpn(null,null);
            ret.vpn = defaultVpn;
            string[] splitstr = user_vpn.Split('@');
            ret.user = splitstr[0];
            if (splitstr.Length == 2)
                ret.vpn = splitstr[1];
            return ret;
        }

          // Callback handlers handle events received from the session and message flows.
        #region Callback Handlers
        /// Simply prints the content of the message to the Console
        public static void PrintMessageEvent(Object source, MessageEventArgs args)
        {
            IMessage oMsg = args.Message;
            string msg = string.Format("Received message id={0}", oMsg.ADMessageId);
            msg += string.Format("\n\tDeliveryMode: {0}", oMsg.DeliveryMode);
            if (oMsg.Destination != null)
                msg += string.Format("\n\tDestination: {0}", oMsg.Destination.Name);
            if (oMsg.XmlContent != null)
                msg += string.Format("\n\tXmlContent: {0} bytes", oMsg.XmlContent.Length);
            if (oMsg.BinaryAttachment != null)
                msg += string.Format("\n\tBinaryPayload: {0} bytes", oMsg.BinaryAttachment.Length);
            IList<long> consumerIds = oMsg.ConsumerIdList;
            if (consumerIds != null)
            {
                msg += string.Format("\n\tConsumerIds: {0}", consumerIds.ToString());
            }
            Console.WriteLine("Received: \n"  + oMsg.Dump());
        }

        /// HandleMessageEvent is the callback method that is specified 
        /// to handle messages received from the Solace appliance. Notice that 
        /// depending on the message type there will be different
        /// message parts.
        /// Note: This code is executed within the API thread and 
        /// should deal with the message quickly or queue the message 
        /// for further processing in another thread.
        public static void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            // Prints the message
            PrintMessageEvent(source, args);
            // It's recommended to Dispose a received message to free up heap memory explicitly
            args.Message.Dispose();
        }

        /// HandleSessionEvent is the callback method that is specified 
        /// to handle session events.  Session events are things like:
        ///     Link to Solace appliance is UP
        ///     Link to Solace appliance is DOWN
        ///     Subscription is invalid and rejected by Solace Appliance.
        ///  Note: This code is executed within the API thread and 
        ///  should deal with the event quickly or queue the event for 
        ///  further processing in another thread.
        public static void HandleSessionEvent(Object sender, SessionEventArgs args)
        {
            Console.WriteLine(string.Format("Session Event Received: '{0}' Type: '{1}' Text: '{2}' CorrelationTag: '{3}'",
                 args.Event,
                 args.ResponseCode.ToString(),
                 args.Info,
                 args.CorrelationKey));
        }

        /// HandleFlowEvent is the callback method that is specified 
        /// to handle flow session events. Flow events are things like:
        ///     Assured message flow to Solace is UP
        ///     Assured message flow to Solace is DOWN
        ///     DTE is invalid on Solace Appliance
        ///   Note: This code is executed within the API thread and 
        ///   should deal with the event quickly or queue the event for 
        ///   further processing in another thread.
        public static void HandleFlowEvent(Object sender, FlowEventArgs args)
        {
            Console.WriteLine(string.Format("Flow Event Received: '{0}' Type: '{1}' Text: '{2}'",
                args.Event,
                args.ResponseCode.ToString(),
                args.Info));
        }
        #endregion

        /*
         * Get time in usecs (microseconds).
         */
        public static long getTimeInUs()
        {
            return (long) HighResolutionCounter.TotalMicroSeconds(HighResolutionCounter.GetTickCount());
        }

        /*
         * Recursive map printer for displaying a map to the user.
         */
        public static string dumpMap(IMapContainer m, int indent)
        {
            string pad = getSpaces(indent);
            string ret = pad + "(Dumping map)\n";
            KeyValuePair<string, ISDTField> e;
            while ((e = m.GetNext()).Key != null)
            {
                ret += pad + string.Format("{0} : [Type={1}, Val={2}]\n", e.Key, e.Value.Type, e.Value.Value);
                if (e.Value.Type == SDTFieldType.MAP)
                {
                    ret += dumpMap((IMapContainer)e.Value.Value, indent + 4);
                }
                else if (e.Value.Type == SDTFieldType.STREAM)
                {
                    ret += dumpStream((IStreamContainer)e.Value.Value, indent + 4);
                }
            }
            m.Rewind();
            return ret;
        }

        /*
         * Recursive stream printer for displaying a stream to the user.
         */
        public static string dumpStream(IStreamContainer s, int indent)
        {
            string pad = getSpaces(indent);
            string ret = pad + "(Dumping stream)\n";
            ISDTField e;
            while ((e = s.GetNext()) != null)
            {
                ret += pad + string.Format("[Type={0}, Val={1}]\n", e.Type, e.Value);
                if (e.Type == SDTFieldType.MAP)
                {
                    ret += dumpMap((IMapContainer)e.Value, indent + 4);
                }
                else if (e.Type == SDTFieldType.STREAM)
                {
                    ret += dumpStream((IStreamContainer)e.Value, indent + 4);
                }
            }
            s.Rewind();
            return ret;
        }

        /* Whitespace generator */
        public static string getSpaces(int n)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < n; i++)
                sb.Append(" ");
            return sb.ToString();
        }

        /* A custom-defined exception type to wrap a API exception */
        public class SampleException : Exception
        {
            public SampleException(string message, Exception cause) : base(message, cause)
            {
            }
        }

        /* Utility method to check a ReturnCode for OK; throw otherwise */
        public static void checkRcOK(ReturnCode rc, string message)
        {
            if (rc != ReturnCode.SOLCLIENT_OK)
            {
                throw new SampleException(string.Format(message + " ReturnCode was '{0}'", rc.ToString()), null);
            }
        }

        /// <summary>
        /// Prints the Rx statistics.
        /// </summary>
        /// <param name="stats"></param>
        public static void PrintRxStats(IDictionary<Stats_Rx, Int64> stats)
        {
            StringBuilder sb = new StringBuilder();
            Stats_Rx[] statsValues = (Stats_Rx[])Enum.GetValues(typeof(Stats_Rx));
            sb.Append("Session Rx stats: ");
            for (int i = 0; i < statsValues.Length; i++)
            {
                String value = "n/a";
                try
                {
                    value = "" + stats[statsValues[i]];
                }
                catch (Exception)
                {

                }
                sb.Append(string.Format("\n\t{0}: {1}", statsValues[i], value));
            }
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Prints the Rx statistics.
        /// </summary>
        /// <param name="stats"></param>
        public static void PrintTxStats(IDictionary<Stats_Tx, Int64> stats)
        {
            StringBuilder sb = new StringBuilder();
            Stats_Tx[] statsValues = (Stats_Tx[])Enum.GetValues(typeof(Stats_Tx));
            sb.Append("Session Tx stats: ");
            for (int i = 0; i < statsValues.Length; i++)
            {
                String value = "n/a";
                try
                {
                    value = "" + stats[statsValues[i]];
                }
                catch (Exception)
                {

                }
                sb.Append(string.Format("\n\t{0}: {1}", statsValues[i], value));
            }
            Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// Creates a basic message given a SessionConfiguration.
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
        public static IMessage CreateMessage(SessionConfiguration sc, ISession session)
        {
            IDestination dest = null;
            if (sc.DestMode == DestMode.QUEUE)
            {
                dest = ContextFactory.Instance.CreateQueue(SampleUtils.SAMPLE_QUEUE);
            }
            else if (sc.DestMode == DestMode.TOPIC)
            {
                dest = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
            }
            IMessage msg = ContextFactory.Instance.CreateMessage();
            if (sc.DestMode == DestMode.CONTENT)
            {
                msg.XmlContent = Encoding.ASCII.GetBytes(SampleUtils.MSG_XMLDOC);
            }
            else
            {
                msg.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
            }
            msg.DeliveryMode = sc.DeliveryMode;
            if (dest != null)
            {
                msg.Destination = dest;
            }
            return msg;
        }
    }
}
