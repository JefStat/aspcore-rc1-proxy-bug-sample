using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace sample
{
    public class Proxy
    {
        private readonly RequestDelegate next;
        private readonly ILogger logger;
		private readonly Uri proxyHost = new Uri("https://SOMETLS1URI");
        private readonly HttpClient httpClient;


		public Proxy(RequestDelegate next, ILoggerFactory loggerFactory)
        {
			logger = loggerFactory?.CreateLogger<Proxy>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.next = next ?? throw new ArgumentNullException(nameof(next));
            httpClient = new HttpClient();
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path;

            const string proxyRoute = "/ABCDEFGHIJKLMNOPQRSTUV";
//            if (!path.Value.StartsWith(proxyRoute))
//            {
//                //continues through the rest of the pipeline
//                await next.Invoke(context);
//                return;
//            }

            var requestMessage = new HttpRequestMessage();
            if (!string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Request.Method, "HEAD", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Request.Method, "DELETE", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(context.Request.Method, "TRACE", StringComparison.OrdinalIgnoreCase))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            // currently broken in aspcore 2.1-rc1 https://github.com/dotnet/corefx/issues/29771
            foreach (var header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            var uri = new Uri(proxyHost, $"api{path.Value.Replace(proxyRoute, "")}");
            requestMessage.Method = new HttpMethod(context.Request.Method);
            requestMessage.RequestUri = uri;
            try
            {
                using (var responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
                {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    foreach (var header in responseMessage.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    foreach (var header in responseMessage.Content.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }

                    // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
                    context.Response.Headers.Remove("transfer-encoding");
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                }
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync(e.ToString());
            }
        }
    }

    public static class ProxyExtensions
    {
        public static IApplicationBuilder UseProxy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<Proxy>();
        }
    }
}
