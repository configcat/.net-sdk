using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ConfigCat.Client.Tests
{
    internal sealed class ExceptionThrowerHttpClientHandler : HttpClientHandler
    {
        private readonly Exception exception;

        public ExceptionThrowerHttpClientHandler(Exception ex = null)
        {
            this.exception = ex ?? new NotImplementedException();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw exception;
        }
    }
}
