using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoWifiLogin
{
    public class Program
    {
        private static async Task<int> Main(string[] args)
        {
            await Parser.Default.ParseArguments<Options>(args)
                .WithParsedAsync(Run).ConfigureAwait(false);
            return 0;
        }

        private static async Task Run(Options option)
        {
            var builder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHttpClient<IWifiService, WifiService>();
                })
                .UseConsoleLifetime();

            var host = builder.Build();

            using var serviceScope = host.Services.CreateScope();
            {
                var services = serviceScope.ServiceProvider;
                try
                {
                    var myService = services.GetRequiredService<IWifiService>();
                    Console.WriteLine(await myService.Login(option.Username, option.Password, option.Type is MachineType.Win).ConfigureAwait(false)
                        ? "Login Success"
                        : "Login Failed");
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred.");
                }
            }
        }

        private class Options
        {
            [Option('u', "username", Required = true, HelpText = "Your account")]
            public string Username { get; set; }

            [Option('p', "password", Required = true, HelpText = "Your password")]
            public string Password { get; set; }

            [Option('t', "Type", Required = false, HelpText = "Runtime Machine [ Mac, Win ], Default: Mac")]
            public MachineType Type { get; set; } = MachineType.Mac;
        }

        private enum MachineType
        {
            Mac,
            Win
        }

        private interface IWifiService
        {
            Task<bool> Login(string username, string password, bool isWin = false);
        }

        public class WifiService : IWifiService
        {
            private HttpClient HttpClient { get; }
            private const string _winWifiRedirectUrl = "http://www.msftconnecttest.com/redirect";
            private const string _macWifiRedirectUrl = "http://captive.apple.com/hotspot-detect.html";

            public WifiService(HttpClient httpClient)
            {
                HttpClient = httpClient;
            }

            public async Task<bool> Login(string username, string password, bool isWin = false)
            {
                if (string.IsNullOrWhiteSpace(username)
                    || string.IsNullOrWhiteSpace(password))
                {
                    throw new ArgumentException($"Please enter {nameof(username)} or {nameof(password)}.");
                }

                var response = isWin
                    ? await HttpClient.GetAsync(_winWifiRedirectUrl).ConfigureAwait(false)
                    : await HttpClient.GetAsync(_macWifiRedirectUrl).ConfigureAwait(false);
                var loginUrl = response.Headers.Location ?? response.RequestMessage.RequestUri;
                if (string.IsNullOrWhiteSpace(loginUrl?.AbsoluteUri)
                    || loginUrl.AbsoluteUri == _winWifiRedirectUrl
                    || loginUrl.AbsoluteUri == _macWifiRedirectUrl)
                {
                    return true;
                }

                var magic = HttpUtility.ParseQueryString(loginUrl.Query).ToString();
                if (string.IsNullOrWhiteSpace(magic))
                {
                    throw new KeyNotFoundException(nameof(magic));
                }

                var loginResponse = await HttpClient.PostAsync(
                    new Uri($"{loginUrl.Scheme}://{loginUrl.Authority}"),
                    new FormUrlEncodedContent(new[]
                        {
                            new KeyValuePair<string, string>("magic", magic),
                            new KeyValuePair<string, string>("username", username),
                            new KeyValuePair<string, string>("password", password),
                            new KeyValuePair<string, string>("4Tredir", "https://gss.com.tw")
                        })
                ).ConfigureAwait(false);
                return loginResponse.StatusCode == HttpStatusCode.RedirectMethod
                       || loginResponse.IsSuccessStatusCode;
            }
        }
    }
}
