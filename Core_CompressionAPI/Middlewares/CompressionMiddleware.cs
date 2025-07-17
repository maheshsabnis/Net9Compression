using System.IO.Compression;

namespace Core_CompressionAPI.Middlewares
{
    public class CompressionMiddleware
    {
        private readonly RequestDelegate _next;

        public CompressionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var contentEncoding = context.Request.Headers["Content-Encoding"].ToString();

            context.Request.Body = contentEncoding switch
            {
                "gzip" => new GZipStream(context.Request.Body, CompressionMode.Decompress),
                "deflate" => new DeflateStream(context.Request.Body, CompressionMode.Decompress),
                _ => context.Request.Body
            };


            await _next(context);
        }
    }
}
