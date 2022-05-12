//-----------------------------------------------------------------------------
// FILE:	    SdkArchitecture.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2021 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Polly;
using System.Threading.Tasks;
using Neon.Common;
using Neon.Cryptography;
using RaspberryDebugger.Models.Sdk;

namespace SdkCatalogChecker
{
    /// <summary>
    /// <para>
    /// This program reads the <b>$/sdk-catalog.json</b> file and then downloads
    /// each of the listed SDKs and verifies that the SHA256 hashes match each
    /// download.
    /// </para>
    /// <para>
    /// This should be executed periodically and whenever you edit the catalog
    /// to ensure that the links and hashes are still good.
    /// </para>
    /// </summary>
    public static class Program
    {
        private enum Status
        {
            Ok,
            Error
        }

        /// <summary>
        /// <para>
        /// Verifies the <b>$sdk-catalog.json/</b> SDK catalog file by downloading
        /// the binaries and verifying their SHA512 hashes.
        /// </para>
        /// <note>
        /// This program assumes that it's running within the GitHub repository.
        /// </note>
        /// </summary>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task Main()
        {
            var catalogPath = Path.GetFullPath(Path.Combine(
                Assembly.GetExecutingAssembly().Location, 
                "..", "..", "..", "..", "..", 
                "RaspberryDebugger", 
                "sdk-catalog.json"));

            var catalog     = (SdkCatalog)null;
            var ok          = true;

            Console.WriteLine($"reading: {catalogPath}");

            try
            {
                catalog = NeonHelper.JsonDeserialize<SdkCatalog>(await File.ReadAllTextAsync(catalogPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(NeonHelper.ExceptionError(e));
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.WriteLine($"[{catalog.Items.Count}] catalog items");

            // Verify that all of the links are unique.

            var sdkLinkToItem = new Dictionary<string, SdkCatalogItem>();

            foreach (var item in catalog.Items)
            {
                if (sdkLinkToItem.TryGetValue(item.Link, out var existingItem))
                {
                    ok = false;
                    Console.WriteLine($"SDK [{existingItem.Name}/{existingItem.Architecture}] and [{item.Name}/{item.Architecture}] have the same link: [{item.Link}]");
                    continue;
                }

                sdkLinkToItem.Add(item.Link, item);
            }

            // Verify that all SDK names are unique for a given architecture.
            foreach (var item in catalog.Items)
            {
                if (sdkLinkToItem.TryGetValue($"{item.Name}/{item.Architecture}", out var existingItem))
                {
                    ok = false;
                    Console.WriteLine($"SDK [{existingItem.Name}/{existingItem.Architecture}] is listed multiple times.");
                    continue;
                }

                if (!item.Link.Contains(item.Name))
                {
                    ok = false;
                    Console.WriteLine($"*** ERROR: Link does not include the SDK name: {item.Name}");
                    continue;
                }

                switch (item.Architecture)
                {
                    case SdkArchitecture.Arm32 when item.Link.Contains("arm64"):
                        ok = false;
                        Console.WriteLine("*** ERROR: ARM32 SDK link references a 64-bit SDK.");
                        continue;

                    case SdkArchitecture.Arm64 when item.Link.Contains("arm32"):
                        ok = false;
                        Console.WriteLine("*** ERROR: ARM64 SDK link references a 32-bit SDK.");
                        continue;

                    case SdkArchitecture.Unknown:
                    default:
                        sdkLinkToItem.Add($"{item.Name}/{item.Architecture}", item);
                        break;
                }
            }
            
            // Verify the links and SHA256 hashes.  We're going to do this check in reverse
            // order by .NET version name to verify newer entries first because those will
            // be most likely to be incorrect.

            using var client = new HttpClient();

            // I've seen some transient issues with downloading SDKs from Microsoft: 404 & 503
            // We're going to retry up to 3 times.
            // Wait 10 minutes for low download rates
            var timeOut = TimeSpan.FromMinutes(10);
            client.Timeout = timeOut;

            var watch = new Stopwatch();

            var retryPolicy = Policy
                .Handle<Exception> ()
                .WaitAndRetryAsync(3, _ => timeOut);      // retry 3 times

            foreach (var item in catalog.Items
                         .OrderByDescending(item => SemanticVersion.Parse(item.Version))
                         .ThenBy(item => item.Name)
                         .ThenBy(item => item.Architecture))
            {
                Console.WriteLine();
                Console.WriteLine("----------------------------------------");
                Console.WriteLine();
                Console.WriteLine($"SDK:    {item.Name}/{item.Architecture} (v{item.Version})");
                Console.WriteLine($"Link:   {item.Link}");

                var binary = (byte[])null;

                try
                {
                    watch.Start();

                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        binary = await client.GetByteArraySafeAsync(item.Link);
                    });
                }
                catch (Exception e)
                {
                    Console.WriteLine(NeonHelper.ExceptionError(e));
                }
                finally
                {
                    watch.Stop();
                }

                if (binary == null)
                {
                    ok = false;
                }

                var expectedSha512 = item.Sha512.ToLowerInvariant();
                var actualSha512   = CryptoHelper.ComputeSHA512String(binary).ToLowerInvariant();

                if (actualSha512 == expectedSha512)
                {
                    Console.WriteLine("SHA512: Hashes match");
                    Console.WriteLine($@"Time elapsed: {watch.Elapsed:m\:ss\.ff}");
                }
                else
                {
                    ok = false;

                    Console.WriteLine();
                    Console.WriteLine("*** ERROR: SHA512 hashes don't match!");
                    Console.WriteLine($"Expected: {expectedSha512}");
                    Console.WriteLine($"Actual:   {actualSha512}");
                }

                watch.Reset();
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            Environment.Exit(ok 
                ? Catalog(Status.Ok) 
                : Catalog(Status.Error));
        }

        private static int Catalog(Status status)
        {
            switch (status)
            {
                case Status.Ok:
                    Console.WriteLine("Catalog is OK");
                    Console.WriteLine("Hit any key to close console");
                    Console.ReadKey();

                    return 1;

                case Status.Error:
                default:
                    Console.WriteLine("*** ERROR: One or more catalog items have issues");
                    Console.WriteLine("Hit any key to close console");

                    Console.ReadKey();
                    return 0;
            }
        }
    }
}
