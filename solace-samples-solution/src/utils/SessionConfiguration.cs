using System;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Cache;
using System.Collections.Generic;
using System.Text;

namespace SolaceSystems.Solclient.Examples.Messaging
{
	
    public enum DestMode {
		QUEUE, TOPIC, CONTENT
	}

	public enum RouterMode {
		CR, TR, UNSET
	}

	/// <summary> Container for session properties used in configuring a JCSMPSession.</summary>
	public class SessionConfiguration
	{

		public IpPort IpPort
		{
			get
			{
				return ipPort;
			}
			
			set
			{
				this.ipPort = value;
			}
			
		}
	    public UserVpn RouterUserVpn
		{
			get
			{
				return routerUserVpn;
			}
			
		}
		public MessageDeliveryMode DeliveryMode
		{
			get
			{
				return delMode;
			}
			
			set
			{
				this.delMode = value;
			}
			
		}
		public bool Compression
		{
			get
			{
				return compression;
			}
			
			set
			{
				this.compression = value;
			}
			
		}
		public bool TopicDispatch
		{
			get
			{
				return topicDispatch;
			}
			
			set
			{
				this.topicDispatch = value;
			}
		}

        public DestMode DestMode
        {
            get
            {
                return destMode;
            }
            set
            {
                destMode = value;
            }
        }

        public IDictionary<string , string > ArgBag
        {
            get
            {
                return argBag;
            }
        }

        public string UserPassword
        {
            get
            {
                return routerPassword;
            }
            set
            {
                routerPassword = value;
            }
        }

        public bool UseDurableEndpoint
        {
            get
            {
                return useDurableEndpoint;
            }
            set
            {
                useDurableEndpoint = value;
            }
        }

        public int NumberOfMessagesToPublish
        {
            get
            {
                return numberOfMessagesToPublish;
            }
            set
            {
                numberOfMessagesToPublish = value;
            }
        }

        public Nullable<Int64> startSequenceId
        {
            get
            {
                return this._startSequenceId;
            }
            set
            {
                this._startSequenceId = value;
            }
        }

        public Nullable<Int64> endSequenceId
        {
            get
            {
                return this._endSequenceId;
            }
            set
            {
                this._endSequenceId = value;
            }
        }

        public string trustStoreDirectory;
        public string commonNames;
        public string cipherSuites = "ECDHE-RSA-AES256-GCM-SHA384,ECDHE-RSA-AES256-SHA384,ECDHE-RSA-AES256-SHA,AES256-GCM-SHA384,AES256-SHA256,AES256-SHA,ECDHE-RSA-DES-CBC3-SHA,DES-CBC3-SHA,ECDHE-RSA-AES128-GCM-SHA256,ECDHE-RSA-AES128-SHA256,ECDHE-RSA-AES128-SHA,AES128-GCM-SHA256,AES128-SHA256,AES128-SHA,RC4-SHA,RC4-MD5";
        public string excludedProtocols = "";
        public bool validateCertificate = true;
        public bool validateCertificateDate = true;
        public string sslConnectionDowngradeTo = "";

        public AuthenticationSchemes authenticationScheme;
        public string clientPrivateKeyFile;
        public string clientPrivateKeyPassword;
        public string clientCertificateFile;

        private int numberOfMessagesToPublish = 1;

		private IpPort ipPort;
		
		private UserVpn routerUserVpn;
		
		private string routerPassword;

        private MessageDeliveryMode delMode = MessageDeliveryMode.Direct;
		
		private DestMode destMode;
		
		private IDictionary < string , string > argBag = new Dictionary < string , string >();
		
		private bool topicDispatch;
		
		private bool compression = false;

        private bool useDurableEndpoint = false;

        private Nullable<Int64> _startSequenceId, _endSequenceId;
		
		public  SessionConfiguration SetUserPassword(string routerPassword)
		{
			this.routerPassword = routerPassword;
			return this;
		}
		
		public  SessionConfiguration SetRouterUsername(UserVpn routerUserVpn)
		{
			this.routerUserVpn = routerUserVpn;
			return this;
		}
		
		public  SessionConfiguration SetDestMode(DestMode destMode)
		{
			this.destMode = destMode;
			return this;
		}
		public override string ToString()
		{
			StringBuilder bldr = new StringBuilder();
			bldr.Append("host=");
			bldr.Append(ipPort);
			if (routerUserVpn != null)
			{
				bldr.Append(", username=");
				bldr.Append(routerUserVpn.user);
				if (routerUserVpn.vpn != null)
				{
					bldr.Append(", vpn=");
					bldr.Append(routerUserVpn.vpn);
				}
			}
			bldr.Append(", password=");
			bldr.Append(routerPassword);
			bldr.Append(", compression=");
			bldr.Append(compression);
			return bldr.ToString();
		}
	}
}