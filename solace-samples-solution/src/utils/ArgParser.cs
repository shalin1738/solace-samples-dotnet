using System;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Cache;

namespace SolaceSystems.Solclient.Examples.Messaging
{
	public class ArgParser
	{
		public SessionConfiguration Config
		{
			get
			{
				return sc;
			}
			
			set
			{
				this.sc = value;
			}
			
		}

        public SolLogLevel LogLevel = SolLogLevel.Warning;
        public void setLogLevel(string logstring)
        {
            string logstr = logstring.ToLower();
            switch (logstring.ToLower())
            {
                case "debug":
                    LogLevel = SolLogLevel.Debug;
                    break;
                case "info":
                    LogLevel = SolLogLevel.Info;
                    break;
                case "notice":
                    LogLevel = SolLogLevel.Notice;
                    break;
                case "warn":
                case "warning":
                    LogLevel = SolLogLevel.Warning;
                    break;
                case "error":                
                    LogLevel = SolLogLevel.Error;
                    break;
                case "critical":
                    LogLevel = SolLogLevel.Critical;
                    break;
                case "alert":
                    LogLevel = SolLogLevel.Critical;
                    break;
                default:
                     throw new ArgumentOutOfRangeException("Unable to parse " + logstring);
            }
        }
        
        public static string CommonUsage
		{
			get
			{
				string str = "Common parameters:\n";
				str += "\t -h HOST[:PORT]  Appliance IP address [:port, omit for default]\n";
				str += "\t -u USER[@VPN]   Authentication username [@vpn, omit for default]\n";
				str += "\t[-w PASSWORD]    Authentication password\n";
                str += "\t[-l logLevel]    API and Application logging level (debug, info, notice, warn, error, critical)\n";
                str += "\t[-z]             Enable compression (SolOS-TR appliances only)\n";
				return str;
			}
			
		}
		public static string CacheArgUsage
		{
			get
			{
				StringBuilder buf = new StringBuilder();
				buf.Append(ArgParser.CommonUsage);
				buf.Append("Cache request parameters:\n");
                buf.Append("\t -c CACHE_NAME  Name of cache for cache request\n");
				buf.Append("\t[-m MAX_MSGS]   Maximum messages per topic to retrieve (default 1)\n");
				buf.Append("\t[-a MAX_AGE]    Maximum age of messages to retrieve (default 0)\n");
				buf.Append("\t[-o TIMEOUT]    Cache request timeout in ms (default 5000)\n");
				buf.Append("\t[-s SUBSCRIBE]  Subscribe to cache topic (default false)\n");
                buf.Append("\t[-d ACTION]     Live data action (default FLOW_THRU)\n");
                buf.Append("\t[-l LOGLEVEL]   API and Application logging level (debug, info, notice, warn, error, critical)\n");
				return buf.ToString();
			}
			
		}
		internal SessionConfiguration sc;
		
		
		/// <summary> 
        /// Parse command-line: the common params are stored in dedicated
		/// SessionConfiguration fields, while program-specific params go into the
		/// argBag map field.
		/// </summary>
		public bool Parse(string[] args)
		{
			if (Config == null)
			{
				Config = new SessionConfiguration();
			}
			return Parse(args, this.sc);
		}

        public bool Parse(string[] args, SessionConfiguration sc)
		{
			try
			{
				for (int i = 0; i < args.Length; i++)
				{
                    switch (args[i]) 
                    {
                        case "-h":
						    i++;
						    sc.IpPort = IpPort.Parse(args[i]);
                            break;
                        case "-u":
						    i++;
						    sc.SetRouterUsername(UserVpn.Parse(args[i]));
                            break;
                        case "-w":
						    i++;
						    sc.SetUserPassword(args[i]);
                            break;
                        case "-z":
						    sc.Compression = true;
                            break;
                        case "-t":
						    i++;
						    string dm = args[i].ToLower();
						    MessageDeliveryMode? dmobj = ParseDeliveryMode(dm);
						    if (dmobj != null)
							    sc.DeliveryMode = dmobj.Value;
						    else
							    return false; // err
                            break;
                        case "-l":
                            i++;
                            setLogLevel(args[i]);
                            break;
                        case "--durable":
                            sc.UseDurableEndpoint = true;
                            break;
                        case "-help":
                            return false; // err: print help
                        default:
						    string str_key = args[i];
						    string str_value = "";
						    if (i + 1 < args.Length)
						    {
							    string str_tmpvalue = args[i + 1]; // lookahead
							    if (!str_tmpvalue.StartsWith("-"))
							    {
								    // we have a value!
								    i++;
								    str_value = args[i];
							    }
						    }
						    sc.ArgBag.Add(str_key, str_value);
                            break;
                    }
				}
			}
			catch (Exception)
			{
				return false; // err
			}

            bool clientCertUsed = false;
            if (sc.ArgBag.ContainsKey("-a"))
            {
                if (sc.ArgBag["-a"].Equals("CLIENT_CERTIFICATE"))
                {
                    clientCertUsed = true;
                }
            }

			if (sc.IpPort == null)
			{
				return false; // err
			}

            if (!clientCertUsed)
            {
                //Required when client certificates aren't used.
                if (sc.RouterUserVpn == null)
                {
                    
                    return false;
                }

                //Required when client certificates aren't used.
                if (sc.RouterUserVpn.user.Length == 0)
                {
                    return false;
                }
            }
			
            // Disable certificate validation for all samples (Except secureSession which
            // will set this value from its own argument parser).
            sc.validateCertificate = false;

			return true; // success
		}
		
		public bool ParseCacheSampleArgs(string[] args)
		{
			CacheSessionConfiguration cf = new CacheSessionConfiguration();
			this.sc = cf;
			Parse(args, this.sc); //Parse common arguments
			
			for (int i = 0; i < args.Length; i++)
			{
                switch (args[i]) 
                {
                    case "-c":
                        i++;
					    if (i >= args.Length)
						    return false;
					    cf.CacheName = args[i];
                        break;
                    case "-m":
					    i++;
					    if (i >= args.Length)
                            return false;
					    cf.MaxMsgs = System.Int32.Parse(args[i]);
                        break;
                    case "-a":
					    i++;
					    if (i >= args.Length)
                            return false;
					    cf.MaxAge = Int32.Parse(args[i]);
                        break;
                    case "-l":
                        i++;
                        setLogLevel(args[i]);
                        break;
                    case "-o":
					    i++;
					    if (i >= args.Length)
                            return false;
					    cf.Timeout = System.Int32.Parse(args[i]);
                        break;
                    case "-s":
					    i++;
					    if (i >= args.Length)
                            return false;
					    cf.Subscribe = Boolean.Parse(args[i]);
                        break;
                    case "-d":
					    i++;
					    if (i >= args.Length)
                            return false;
                        cf.Action = ParseCacheLiveDataAction(args[i]);
                        break;
                    default:
                        break;
                }
			}
			
			if (cf.CacheName == null)
			{
				System.Console.Error.WriteLine("No cache name specified");
                return false;
			}
			return true;
		}

        private ITopic ParseTopic(string str)
        {
            return ContextFactory.Instance.CreateTopic(str);
        }

        private CacheLiveDataAction ParseCacheLiveDataAction(string cacheLiveDataActionStr)
        {
            return CacheLiveDataAction.FLOW_THRU;
        }
		
		public static MessageDeliveryMode? ParseDeliveryMode(string dm)
		{
			if (dm == null)
				return null;
			dm = dm.ToLower();
            switch (dm)
            {
                case "direct":
                    return MessageDeliveryMode.Direct;
                case "persistent":
                    return MessageDeliveryMode.Persistent;
                case "non-persistent":
                    return MessageDeliveryMode.NonPersistent;
                default:
                    return null;
            }
		}
	}
}