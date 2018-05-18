using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Xunit.Abstractions;

namespace sample
{
	public class SampleTest : ControllerTest
	{
		public SampleTest(ITestOutputHelper output) : base(output) { }

		[Fact]
		public async Task GetSomeProxyRoute()
		{
			var response = await HttpClient.GetAsync("/ABCDEFGHIJKLMNOPQRSTUV/Someotherroute");

			response.EnsureSuccessStatusCode();
			var value = await response.Content.ReadAsStringAsync();
		}
	}

	public abstract class ControllerTest
	{
		private static TestServer testServer => PickClient.TestServer;
		public HttpClient HttpClient => PickClient.HttpClient;

		protected readonly ITestOutputHelper Output;

		protected ControllerTest()
		{
		}

		protected ControllerTest(ITestOutputHelper output) : this() { this.Output = output; }

	}

	public static class PickClient
	{
		private static TestServer _testServer;
		public static TestServer TestServer
		{
			get
			{
				_testServer = _testServer ?? new TestServer(
								  new WebHostBuilder()
									  .UseContentRoot("")
									  .UseEnvironment("Development")
									  .UseStartup<Startup>());
				return _testServer;
			}
		}

		private static HttpClient _httpClient;
		public static HttpClient HttpClient
		{
			get
			{
				_httpClient = _httpClient ?? TestServer.CreateClient();
				return _httpClient;
			}
		}
	}
}
