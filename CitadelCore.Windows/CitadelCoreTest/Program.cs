﻿/*
* Copyright © 2017 Jesse Nicholson
* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

using CitadelCore.Logging;
using CitadelCore.Net.Proxy;
using CitadelCore.Windows.Net.Proxy;
using System;
using System.IO;
using System.Threading;

namespace CitadelCoreTest
{
    internal class Program
    {
        private static byte[] s_blockPageBytes;

        private static bool OnFirewallCheck(string binaryAbsPath)
        {
            // Only filter chrome.
            return binaryAbsPath.IndexOf("chrome", StringComparison.OrdinalIgnoreCase) != -1;
        }

        private static void OnMsgBegin(Uri reqUrl, string headers, byte[] body, MessageType msgType, MessageDirection msgDirection, out ProxyNextAction nextAction, out string customBlockResponseContentType, out byte[] customBlockResponse)
        {
            if(reqUrl.Host.Equals("777.com", StringComparison.OrdinalIgnoreCase))
            {
                nextAction = ProxyNextAction.DropConnection;
                customBlockResponseContentType = "text/html";
                customBlockResponse = s_blockPageBytes;
                return;
            }

            nextAction = ProxyNextAction.AllowAndIgnoreContent;
            customBlockResponseContentType = string.Empty;
            customBlockResponse = null;
        }

        private static void OnMsgEnd(Uri reqUrl, string headers, byte[] body, MessageType msgType, MessageDirection msgDirection, out bool shouldBlock, out string customBlockResponseContentType, out byte[] customBlockResponse)
        {
            Console.WriteLine(nameof(OnMsgEnd));

            shouldBlock = false;
            customBlockResponseContentType = string.Empty;
            customBlockResponse = null;
        }

        private static void Main(string[] args)
        {
            s_blockPageBytes = File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BlockedPage.html"));
            // Let the user decide when to quit with ctrl+c.
            var manualResetEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                manualResetEvent.Set();
                Console.WriteLine("Shutting Down");
            };

            // Hooking into these properties gives us an abstract interface where we may
            // use informational, warning and error messages generated by the internals of
            // the proxy in whatsoever way we see fit, though the design was to allow users
            // to choose logging mechanisms.
            LoggerProxy.Default.OnInfo += (msg) =>
            {
                Console.WriteLine("INFO: {0}", msg);
            };

            LoggerProxy.Default.OnWarning += (msg) =>
            {
                Console.WriteLine("WARN: {0}", msg);
            };

            LoggerProxy.Default.OnError += (msg) =>
            {
                Console.WriteLine("ERRO: {0}", msg);
            };
            
            // Just create the server.
            var proxyServer = new WindowsProxyServer(OnFirewallCheck, OnMsgBegin, OnMsgEnd);
            
            // Give it a kick.
            proxyServer.Start();

            // And you're up and running.
            Console.WriteLine("Proxy Running");

            Console.WriteLine("Listening for IPv4 HTTP connections on port {0}.", proxyServer.V4HttpEndpoint.Port);
            Console.WriteLine("Listening for IPv4 HTTPS connections on port {0}.", proxyServer.V4HttpsEndpoint.Port);
            Console.WriteLine("Listening for IPv6 HTTP connections on port {0}.", proxyServer.V6HttpEndpoint.Port);
            Console.WriteLine("Listening for IPv6 HTTPS connections on port {0}.", proxyServer.V6HttpsEndpoint.Port);

            // Don't exit on me yet fam.
            manualResetEvent.WaitOne();

            Console.WriteLine("Exiting.");

            // Stop if you must.
            proxyServer.Stop();
        }
    }
}