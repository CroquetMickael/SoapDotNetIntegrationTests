using System;

namespace MyApi.WebApi.Tests.Utils;

public class HttpMessageHandlerMock : HttpMessageHandler
{
    private HttpResponseMessage? _httpResponseMessage;

    public void SetResponse(HttpResponseMessage httpResponseMessage)
    {
        _httpResponseMessage = httpResponseMessage;
    }

    public void SetFailedAttemptsAndResponse(HttpResponseMessage httpResponseMessage)
    {
        _httpResponseMessage = httpResponseMessage;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_httpResponseMessage == null)
        {
            throw new InvalidOperationException("Error in request");
        }

        return Task.FromResult(_httpResponseMessage);
    }
}
