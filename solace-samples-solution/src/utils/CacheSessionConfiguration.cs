/*
* Copyright 2008-2016 Solace Systems, Inc.
* CacheSessionConfiguration.java
*/
using System;
using SolaceSystems.Solclient.Messaging;
using SolaceSystems.Solclient.Messaging.Cache;
using System.Text;

namespace SolaceSystems.Solclient.Examples.Messaging
{
	
	/// <summary> Container for cache session properties used in configuring a CacheSession.</summary>
	public class CacheSessionConfiguration:SessionConfiguration
	{
		public string CacheName
		{
			get
			{
				return mCacheName;
			}
			
			set
			{
				mCacheName = value;
			}
			
		}
		
        public int MaxAge
		{
			get
			{
				return mMaxAge;
			}
			
			set
			{
				mMaxAge = value;
			}
			
		}
		public int MaxMsgs
		{
			get
			{
				return mMaxMsgs;
			}
			
			set
			{
				mMaxMsgs = value;
			}
			
		}
		public int Timeout
		{
			get
			{
				return mTimeout;
			}
			
			set
			{
				mTimeout = value;
			}
			
		}
		public bool Subscribe
		{
			get
			{
				return mSubscribe;
			}
			
			set
			{
				mSubscribe = value;
			}
			
		}
		public CacheLiveDataAction Action
		{
			get
			{
				return mAction;
			}
			
			set
			{
				mAction = value;
			}
			
		}

		private string mCacheName = null;
		private int mMaxAge = 0;
		private int mMaxMsgs = 1;
		private int mTimeout = 5000;
		private ITopic mTopic = null;
		private bool mSubscribe = false;
		private CacheLiveDataAction mAction = CacheLiveDataAction.FLOW_THRU;

		public override string ToString()
		{
			StringBuilder bldr = new StringBuilder(base.ToString());
			bldr.Append(", cacheName=");
			bldr.Append(mCacheName);
			bldr.Append(", maxAge=");
			bldr.Append(mMaxAge);
			bldr.Append(", maxMsgs=");
			bldr.Append(mMaxMsgs);
			bldr.Append(", Timeout=");
			bldr.Append(mTimeout);
			bldr.Append(", topic=");
			bldr.Append(mTopic);
			bldr.Append(", subscribe=");
			bldr.Append(mSubscribe);
			bldr.Append(", action=");
			bldr.Append(mAction);
			return bldr.ToString();
		}
	}
}