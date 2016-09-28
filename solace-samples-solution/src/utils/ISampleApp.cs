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

namespace SolaceSystems.Solclient.Examples.Messaging
{
    interface ISampleApp
    {

        /// <summary>
        /// Main entry point, called from the sample's main application
        /// </summary>
        /// <param name="args"></param>
        void Call(string[] args);

        /// <summary>
        /// A short text describing what the sample does
        /// </summary>
        /// <returns></returns>
        string ShortDescription();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="extraOptionsFroCommonArgs">
        /// If the sample uses extra arguments in addition to the common ones.
        /// When GetIsUsingCommonArgs returns false, this string is ignored.
        /// </param>
        /// <param name="sampleSpecificUsge">
        /// When this sample does not use the common command line arguments,
        /// it will return false and this string is displayed to the user instead 
        /// of the common args.
        /// </param>
        /// <returns> true if the sample uses common args, false otherwise</returns>
        bool GetIsUsingCommonArgs(out string extraOptionsFroCommonArgs, out string sampleSpecificUsge);


    }
}
