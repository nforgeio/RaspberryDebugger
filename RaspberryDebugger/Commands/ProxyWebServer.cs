using Neon.SSH;
using Polly;
using System;

namespace RaspberryDebugger.Commands
{
    internal enum WebServer
    {
        None,
        Kestrel,
        Other
    }

    internal static class ProxyWebServer
    {
        /// <summary>
        /// Listen for proxy server or krestel
        /// </summary>
        /// <param name="aspPort">Given ASP port number</param>
        /// <param name="connection">SSH proxy connection</param>
        /// <param name="webServer">Which web server to listen for</param>
        /// <returns>true if found and which web server detected</returns>
        public static (bool, WebServer) ListenFor(int aspPort, LinuxSshProxy connection, WebServer webServer)
        {
            return webServer == WebServer.Kestrel
                ? SearchKrestel(aspPort, connection) 
                : SearchReverseProxy(aspPort, connection);
        }

        /// <summary>
        /// Search for Krestel
        /// </summary>
        /// <param name="aspPort">Given ASP Port number</param>
        /// <param name="connection">SSH proxy connection</param>
        /// <returns>true if found and which one detected</returns>
        private static (bool, WebServer) SearchKrestel(int aspPort, LinuxSshProxy connection)
        {
            // search for dotnet kestrel web server
            var appKestrelListeningScript =
                $@"
                    if lsof -i -P -n | grep --quiet 'dotnet\|TCP\|:{aspPort}' ; then
                        exit 0
                    else
                        exit 1
                    fi
                ";

            var response = ExecSudoCmd(appKestrelListeningScript, connection);

            return response.ExitCode == 0
                ? (true, WebServer.Kestrel)
                : (false, WebServer.None);
        }

        /// <summary>
        /// Search for reverse proxy: Apache, NGiNX, etc.
        /// </summary>
        /// <param name="aspPort">Given ASP port number</param>
        /// <param name="connection">SSH proxy connection</param>
        /// <returns>true if found and which one detected</returns>
        private static (bool, WebServer) SearchReverseProxy(int aspPort, LinuxSshProxy connection)
        {
            // search for web server running as reverse proxy
            var appWebServerListeningScript =
                $@"
                    if lsof -i -P -n | grep --quiet 'TCP 127.0.0.1:{aspPort}' ; then
                        exit 0
                    else
                        exit 1
                    fi
                ";

            var response = ExecSudoCmd(appWebServerListeningScript, connection);

            return response.ExitCode == 0
                ? (true, WebServer.Other)
                : (false, WebServer.None);
        }

        /// <summary>
        /// Execute command with sudo and retries
        /// </summary>
        /// <param name="cmd">Execute this command</param>
        /// <param name="connection">Linux ssh</param>
        /// <returns>Command response</returns>
        private static CommandResponse ExecSudoCmd(string cmd, LinuxSshProxy connection)
        {
            var retryPolicy = Policy
                .HandleResult<CommandResponse>(ret => ret.ExitCode != 0)
                .WaitAndRetry(3, _ => TimeSpan.FromMilliseconds(200));

            return retryPolicy.Execute(() =>
                connection.SudoCommand(CommandBundle.FromScript(cmd)));
        }
    }
}
