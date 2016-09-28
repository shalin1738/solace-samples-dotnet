            Solace Systems Messaging API for .NET Sample Programs

INTRODUCTION

   These  samples  provide  a  basic introduction to using the Messaging API
   for .NET in messaging applications. Common uses, such as sending a message, 
   receiving  a message, asynchronous messaging, and subscription management,
   are described in detail in these samples.

   Before working with these samples, ensure that you have read and understood
   the  basic  concepts  found in the Messaging APIs Developer Guide.


SOFTWARE REQUIREMENTS
   
   .NET 2.0 or above
   VS 2005 or above to use the bundled VS solutions and project files
   (For convenience, a VS 2010 solution file is also provided)


INTRODUCTORY SAMPLES LIST

   The following introductory samples are included:

	ActiveFlowIndication        Demonstrate active flow indication events.
	AdPubAck                    AD publisher with replication
	AsyncCacheRequest           Asynchronous cache request sample
	DirectPubSub                Direct Delivery Mode Publish and Subscribe
	DTOPubSub                   DTO sample using publishers and subscribers
	EventMonitor                Demonstrates event monitoring over the message bus
	MessageSelectorsOnQueue     Simple subscriber with selector
	MessageTTLAndDeadMessageQueueMessage TTL, Expiration and DMQ
	NoLocalPubSub               No Local Pub/Sub Sample
	QueueProvisionAndBrowse     Queue provision and browse
	RedirectLogs                Demonstrates redirecting API logs to NLog
	Replication                 AD publisher with replication
	RRDirectReplier             An API sample that demonstrates a replier using direct messaging
	RRDirectRequester           An API sample that demonstrates a requester using direct messaging
	RRGuaranteedReplier         An API sample that demonstrates a replier using guaranteed messaging
	RRGuaranteedRequester       An API sample that demonstrates a requester using guaranteed messaging
	SDTPubSubMsgDep             Publisher and subscriber of message-dependent SDT messages
	SDTPubSubMsgIndep           Publisher and subscriber of message independent SDT messages
	SecureSession               Direct Delivery Mode Publish and Subscribe over SSL
	SempGetOverMB               Sample using SEMP over the message bus
	SempHttpSetRequest          SEMP sample using SEMP over HTTP
	SempPagingRequests          Sample using SEMP over the message bus
	SimpleFlowToQueue           Flow to a temporary or non-temporary Queue
	SimpleFlowToTopic           Simple flow to a temporary or non-temporary Topic.
	SubscribeOnBehalfOfClient   Subscribe on behalf of another client.
	SyncCacheRequest            Synchronous cache request sample
	TopicDispatch               Topic dispatch sample
	TopicToQueueMapping         Topic to queue mapping
	Transactions                Demonstrates the use of transactions
        
HOW TO BUILD THE SAMPLES

   Use the solution file cs_sdk_examples.sln or cs_sdk_examples-vs2010.sln to build the samples.
   

HOW TO RUN THE SAMPLES

Launch SolClientSamples.exe from the bin/Release directory or 
SolClientSamples_64.exe from the bin/x64_Release directory. 
Usage:

<SolClientSamples executable> <SAMPLENAME> <SAMPLE ARGS...>
Where:
<SAMPLENAME> is the sample app to execute.
<SAMPLE ARGS...> are arguments to pass to this sample application.
Specify SAMPLENAME with no further arguments to get usage information for the
selected sample.


Copyright 2009-2016 Solace Systems, Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to use and copy the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
UNLESS STATED ELSEWHERE BETWEEN YOU AND SOLACE SYSTEMS, INC., THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE
