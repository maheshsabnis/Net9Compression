

using System.IO.Compression;
using System.Net;

namespace Blaz_Client.CompressionService
{
    public class CompressionAgent : HttpContent
    {
        private readonly HttpContent _originalRequestContent;
        private readonly string _encodingType;
        public CompressionAgent(HttpContent originalContent, string encodingType)
        {
            _originalRequestContent = originalContent ?? throw new ArgumentNullException(nameof(originalContent));
            _encodingType = encodingType ?? throw new ArgumentNullException(nameof(encodingType));

            // Read Header Keys and Values
            foreach (var header in _originalRequestContent.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            Headers.ContentEncoding.Add(_encodingType);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            // Compress the original content based on the encoding type

            Stream compressesStream = _encodingType switch
            {
                "gzip" => new GZipStream(stream, CompressionMode.Compress, true),
                "deflate" => new DeflateStream(stream, CompressionMode.Compress, true),
                _ => throw new NotSupportedException($"Encoding type '{_encodingType}' is not supported.")
            };
            await _originalRequestContent.CopyToAsync(compressesStream);
            await compressesStream.FlushAsync();


        }

        protected override bool TryComputeLength(out long length)
        {
           length = -1; // Length is not computable for compressed content
            return false;
        }
    }
}
