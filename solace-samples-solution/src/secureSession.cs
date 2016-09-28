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
// 
//                              * secureSession *
//
// This sample demonstrates:
//  - Creating a SSL session using parameters specified by arguments.
//  - Subscribing to a Topic for Direct messages.
//  - Publishing direct messages to a Topic.
//  - Receiving messages with a message handler.
//
// This sample shows the basics of creating a context, creating a
// SSL session, connecting a SSL session, subscribing to a topic, and publishing
// direct messages to a Topic. This is meant to be a very basic example, 
// so it uses minimal session properties and a message handler that simply 
// prints any received message to the screen.
// 
// Although other samples make use of common code to perform some of the
// most common actions, this sample explicitly includes many of these common
// methods to emphasize the most basic building blocks of any application.
//
// A server certificate needs to be installed on the appliance and SSL must be
// enabled on the appliance for this sample to work.
// Also, in order to connect to the appliance with Certificate Validation enabled
// (which is enabled by default), the appliance's certificate chain must be signed
// by one of the root CAs in the trust store used by the sample.
//
// For this sample to use CLIENT CERTIFICATE authentication, a trust store has to
// be set up on the appliance and it must contain the root CA that signed the client
// certificate. The VPN must also have client-certificate authentication enabled.
//
// This sample allows the user to pass extra parameters to control how the
// SSL session will get established.  These parameters are available for SSL :
//   -T Full directory path name where the trusted certificates are.  Required when certificate validation is enabled.
//   -N List of comma separated trusted common names.
//   -C List of comma separated cipher suites.
//   -P List of comma separated encryption protocols.
//   -i Disables certificate validation (Enabled by default).
//   -j Disables certificate date validation (Enabled by default).
//   -a authentication scheme (One of : BASIC, CLIENT_CERTIFICATE)]" +
//   -c Path to the client certificate file]" +
//   -k Path to the private key file]" +
//   -p The private key password (Required only when the private key file is protected with a passphrase)]" +


#endregion

using System;
using System.Collections.Generic;
using System.Text;
using SolaceSystems.Solclient.Messaging;
using System.Threading;

namespace SolaceSystems.Solclient.Examples.Messaging.Samples
{
    class SecureSession : SampleApp
    {
        /// <summary>
        /// A short description.
        /// </summary>
        /// <returns></returns>
        public override string ShortDescription()
        {
            return "Direct Delivery Mode Publish and Subscribe over SSL";
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
            sampleSpecificUsage = "Common parameters:\n" +
            "\t -h tcps:HOST[:PORT]  Appliance IP address [:port, omit for default]\n" +
            "\t[-u [USER][@VPN]]   Authentication username (USER is required unless -a CLIENT_CERTIFICATE is specified) [@vpn, omit for default]\n" +
            "\t[-w PASSWORD]    Authentication password\n" +
            "\t[-l logLevel]    API and Application logging level (debug, info, notice, warn, error, critical)\n" +
            "Extra arguments for this sample:\n" +
            "\t[-T Full directory path name where the trusted certificates are.  Required when certificate validation is enabled.] \n" +
            "\t[-N List of comma separated trusted common names.] \n" +
            "\t[-C List of comma separated cipher suites.] \n" +
            "\t[-P List of excluded SSL protocols separated by comma.] \n" +
            "\t[-i Disables certificate validation (Enabled by default).] \n" +
            "\t[-j Disables certificate date validation (Enabled by default).] \n" +
            "\t[-a authentication scheme (One of : BASIC, CLIENT_CERTIFICATE). (Default: BASIC).  NOTE: -u is mandatory when BASIC is specified, but it becomes optional when CLIENT_CERTIFICATE is specified. ]\n" +
            "\t[-c Path to the client certificate file]\n" +
            "\t[-k Path to the private key file]\n" +
            "\t[-p The private key password (Required only when the private key file is protected with a passphrase)]\n" +
            "\t[-d PLAIN_TEXT Downgrade SSL connection to 'PLAIN_TEXT' after client authentication. ]\n\n" +
            "NOTE: Only tcps uri are accepted for the -h parameter";
            return false;
        }

        /// <summary>
        /// Parse the sample's extra command line arguments.
        /// </summary>
        /// <param name="args"></param>
        private bool SampleParseArgs(ArgParser cmdLineParser)
        {
            try
            {
                cmdLineParser.Config.commonNames = cmdLineParser.Config.ArgBag["-N"];
            }
            catch (KeyNotFoundException)
            {
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'N' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.cipherSuites = cmdLineParser.Config.ArgBag["-C"];
            }
            catch (KeyNotFoundException)
            {
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'C' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.excludedProtocols = cmdLineParser.Config.ArgBag["-P"];
            }
            catch (KeyNotFoundException)
            {
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'P' " + ex.Message);
                return false;
            }

            cmdLineParser.Config.validateCertificate = ! cmdLineParser.Config.ArgBag.ContainsKey("-i");
            cmdLineParser.Config.validateCertificateDate = ! cmdLineParser.Config.ArgBag.ContainsKey("-j");

            try {
                cmdLineParser.Config.sslConnectionDowngradeTo = cmdLineParser.Config.ArgBag["-d"];
            }
            catch (KeyNotFoundException)
            {
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'd' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.trustStoreDirectory = cmdLineParser.Config.ArgBag["-T"];
            }
            catch (KeyNotFoundException)
            {
                if (cmdLineParser.Config.validateCertificate && 
                    cmdLineParser.Config.IpPort.ip.StartsWith("tcps:", StringComparison.CurrentCultureIgnoreCase))
                {
                    Console.WriteLine("The truststore must be specified (parameter -T) when certificate validation is enabled.");
                    return false;
                }
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'T' " + ex.Message);
                return false;
            }

            if (cmdLineParser.Config.Compression)
            {
                Console.WriteLine("Compression is not supported with 'tcps' protocol.");
                return false;
            }

            try
            {
                cmdLineParser.Config.authenticationScheme = 
                    (AuthenticationSchemes)Enum.Parse(typeof(AuthenticationSchemes), cmdLineParser.Config.ArgBag["-a"]);
            }
            catch (KeyNotFoundException)
            {
                cmdLineParser.Config.authenticationScheme = AuthenticationSchemes.BASIC;
                // Default
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'a' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.clientPrivateKeyFile = cmdLineParser.Config.ArgBag["-k"];
            }
            catch (KeyNotFoundException)
            {
                if (cmdLineParser.Config.authenticationScheme == AuthenticationSchemes.CLIENT_CERTIFICATE)
                {
                    Console.WriteLine("The private key file must be specified (parameter -k) when client certificate authenticate is used.");
                    return false;
                }

                // Default
                cmdLineParser.Config.clientPrivateKeyFile = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'k' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.clientCertificateFile = cmdLineParser.Config.ArgBag["-c"];
            }
            catch (KeyNotFoundException)
            {
                if (cmdLineParser.Config.authenticationScheme == AuthenticationSchemes.CLIENT_CERTIFICATE)
                {
                    Console.WriteLine("The client certificate file must be specified (parameter -c) when client certificate authenticate is used.");
                    return false;
                }

                // Default
                cmdLineParser.Config.clientCertificateFile = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'c' " + ex.Message);
                return false;
            }

            try
            {
                cmdLineParser.Config.clientPrivateKeyPassword = cmdLineParser.Config.ArgBag["-p"];
            }
            catch (KeyNotFoundException)
            {
                // Default
                cmdLineParser.Config.clientPrivateKeyPassword = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Invalid command line argument 'p' " + ex.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// The message handler is invoked for each direct message received
        /// by the Session. This sample simplly prints the message to the
        /// screen.
        /// 
        /// Message handler code is executed within the API thread, which means
        /// that it should deal with the message quickly or queue the message
        /// for further processing in another thread.
        /// 
        /// Note: In other samples, a common message handler is used. However,
        /// to emphasize this programming paradigm, this sample directly includes 
        /// the message receive handler.
        /// </summary> 
        /// <param name="source"></param>
        /// <param name="args"></param>
        public static void HandleMessageEvent(Object source, MessageEventArgs args)
        {
            IMessage message = args.Message;

            Console.WriteLine("Received message:");
            Console.WriteLine(message.Dump());

            // It is recommended to Dispose a received message to free up heap
            // memory explicitly.
            message.Dispose();
        }

        /// <summary>
        /// The main function in the sample.
        /// </summary>
        /// <param name="args"></param>
        public override void SampleCall(string[] args)
        {
            #region Parse Arguments

            ArgParser cmdLineParser = new ArgParser();
            if (!cmdLineParser.Parse(args))
            {
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            if (!SampleParseArgs(cmdLineParser))
            {
                // Parse failed for sample's arguments.
                PrintUsage(INVALID_ARGUMENTS_ERROR);
                return;
            }
            #endregion

            #region Initialize properties from command line

            ContextProperties contextProps = new ContextProperties();
            SessionProperties sessionProps = SampleUtils.NewSessionPropertiesFromConfig(cmdLineParser.Config);
            sessionProps.Host = cmdLineParser.Config.IpPort.ip;
            sessionProps.UserName = null;
            if (cmdLineParser.Config.RouterUserVpn != null)
            {
                if (cmdLineParser.Config.RouterUserVpn.user.Length != 0)
                {
                    sessionProps.UserName = cmdLineParser.Config.RouterUserVpn.user;
                }
                if (cmdLineParser.Config.RouterUserVpn.vpn != null)
                {
                    sessionProps.VPNName = cmdLineParser.Config.RouterUserVpn.vpn;
                }
            }

            sessionProps.Password = cmdLineParser.Config.UserPassword;

            if (!sessionProps.Host.StartsWith("tcps:", StringComparison.CurrentCultureIgnoreCase))
            {
                PrintUsage("This sample supports secure transport protocols only.");
                return;
            }


            // With reapply subscriptions enabled, the API maintains a
            // cache of added subscriptions in memory. These subscriptions
            // are automatically reapplied following a channel reconnect. 
            sessionProps.ReconnectRetries = 3;
            sessionProps.ReapplySubscriptions = true;

            if (cmdLineParser.Config.Compression)
            {
                // Compression is set as a number from 0-9, where 0 means "disable
                // compression", and 9 means max compression. The default is no
                // compression.
                // Selecting a non-zero compression level auto-selects the
                // compressed SMF port on the appliance, as long as no SMF port is
                // explicitly specified.
                sessionProps.CompressionLevel = 9;
            }

            #endregion

            IContext context = null;
            ISession session = null;
            
            try
            {
                InitContext(cmdLineParser.LogLevel);
                #region Create the Context

                Console.WriteLine("Creating the context ...");
                context = ContextFactory.Instance.CreateContext(contextProps, null);

                #endregion

                #region Create and connect the Session

                Console.WriteLine("Creating the session ...");
                session = context.CreateSession(sessionProps, HandleMessageEvent, SampleUtils.HandleSessionEvent);

                Console.WriteLine("Connecting the session using these SSL properties: ");
                Console.WriteLine("\tCipher Suites : " + sessionProps.SSLCipherSuites);
                Console.WriteLine("\tExcluded Protocols : " + sessionProps.SSLExcludedProtocols);
                if (sessionProps.SSLValidateCertificate)
                {
                    Console.WriteLine("\tTrust store location : " + sessionProps.SSLTrustStoreDir);
                    Console.WriteLine("\tTrusted Common Names : " + sessionProps.SSLTrustedCommonNameList);
                    if (sessionProps.SSLValidateCertificateDate)
                    {
                        Console.WriteLine("\tWill validate the certificate date");
                    }
                    else
                    {
                        Console.WriteLine("\tWill not validate the certificate date");
                    }
                }
                else
                {
                    Console.WriteLine("\tWill not validate the server certificate");
                }

                if (session.Connect() == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Session successfully connected");
                }

                #endregion

                #region Subscribe to the topic

                ITopic topic = ContextFactory.Instance.CreateTopic(SampleUtils.SAMPLE_TOPIC);
                Console.WriteLine("About to subscribe to topic" + topic.ToString());
                if (session.Subscribe(topic, true) == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Successfully added topic subscription");
                }

                #endregion

                #region Publish the messages

                for (int msgsSent = 0; msgsSent < 10; msgsSent++)
                {
                    IMessage message = ContextFactory.Instance.CreateMessage();
                    message.Destination = topic;
                    message.DeliveryMode = MessageDeliveryMode.Direct;
                    message.BinaryAttachment = Encoding.ASCII.GetBytes(SampleUtils.MSG_ATTACHMENTTEXT);
                    session.Send(message);

                    // Wait 1 second between messages. This will also give time for the
                    // final message to be received.
                    Thread.Sleep(1000);
                }

                #endregion

                #region Unsubscribe from the topic

                if (session.Unsubscribe(topic, true) == ReturnCode.SOLCLIENT_OK)
                {
                    Console.WriteLine("Successfully removed topic subscription");
                }

                #endregion

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
