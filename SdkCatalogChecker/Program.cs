//-----------------------------------------------------------------------------
// FILE:	    SdkArchitecture.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Open Source
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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

using Neon.Common;
using Neon.Cryptography;

using Newtonsoft.Json;
using RaspberryDebug;

namespace NetCoreCatalogChecker
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
    public class Program
    {
        /// <summary>
        /// <para>
        /// Verifies the <b>$sdk-catalog.json/</b> SDK catalog file by downloading
        /// the binaries and verifying their SHA512 hashes.
        /// </para>
        /// <note>
        /// This program assumes that it's running within the GitHub repository.
        /// </note>
        /// </summary>
        /// <param name="args">Ignored</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task Main(string[] args)
        {
            var catalogPath = Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, "..", "..", "..", "..", "..", "RaspberryDebug", "sdk-catalog.json"));
            var catalog     = (SdkCatalog)null;
            var allOk       = true;

            Console.WriteLine($"reading: {catalogPath}");

            try
            {
                catalog = NeonHelper.JsonDeserialize<SdkCatalog>(File.ReadAllText(catalogPath));
            }
            catch (Exception e)
            {
                Console.WriteLine(NeonHelper.ExceptionError(e));
                Environment.Exit(1);
            }

            Console.WriteLine();
            Console.WriteLine($"[{catalog.Items.Count}] catalog items");

            using (var client = new HttpClient())
            {
                foreach (var item in catalog.Items
                    .OrderBy(item => item.Name)
                    .ThenBy(item => item.Architecture))
                {
                    Console.WriteLine();
                    Console.WriteLine("----------------------------------------");
                    Console.WriteLine();
                    Console.WriteLine($"SDK:    {item.Name}/{item.Architecture}");
                    Console.WriteLine($"Link:   {item.Link}");

                    if (!item.Link.Contains(item.Name))
                    {
                        allOk = false;
                        Console.WriteLine($"*** ERROR: Link does not include the SDK name: {item.Name}");
                        continue;
                    }

                    if (item.Architecture == SdkArchitecture.ARM32)
                    {
                        if (item.Link.Contains("arm64"))
                        {
                            allOk = false;
                            Console.WriteLine($"*** ERROR: ARM32 SDK link references a 64-bit SDK.");
                            continue;
                        }
                    }
                    else
                    {
                        if (!item.Link.Contains("arm64"))
                        {
                            allOk = false;
                            Console.WriteLine($"*** ERROR: ARM64 SDK link references a 32-bit SDK.");
                            continue;
                        }
                    }

                    var binary = (byte[])null;

                    try
                    {
                        binary = await client.GetByteArraySafeAsync(item.Link);
                    }
                    catch (Exception e)
                    {
                        allOk = false;

                        Console.WriteLine(NeonHelper.ExceptionError(e));
                        continue;
                    }

                    var expectedSha512 = item.SHA512.ToLowerInvariant();
                    var actualSha512   = CryptoHelper.ComputeSHA512String(binary).ToLowerInvariant();

                    if (actualSha512 == expectedSha512)
                    {
                        Console.WriteLine("SHA512: Hashes match");
                    }
                    else
                    {
                        allOk = false;

                        Console.WriteLine();
                        Console.WriteLine($"*** ERROR: SHA512 hashes don't match!");
                        Console.WriteLine($"Expected: {expectedSha512}");
                        Console.WriteLine($"Actual:   {actualSha512}");
                    }
                }
            }

            Console.WriteLine();
            Console.WriteLine("----------------------------------------");
            Console.WriteLine();

            if (allOk)
            {
                Console.WriteLine("Catalog is OK");
                Environment.Exit(1);
            }
            else
            {
                Console.WriteLine("*** ERROR: One or more catalog items have issues");
                Environment.Exit(0);
            }
        }
    }
}
