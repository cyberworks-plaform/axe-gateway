using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Ce.Gateway.Api.Utilities
{
    public class BufferingStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly MemoryStream _bufferedStream;
        private bool _isBuffered;

        public BufferingStream(Stream innerStream)
        {
            _innerStream = innerStream ?? throw new ArgumentNullException(nameof(innerStream));
            _bufferedStream = new MemoryStream();
            _isBuffered = false;
        }

        private async Task BufferStreamAsync()
        {
            if (!_isBuffered)
            {
                await _innerStream.CopyToAsync(_bufferedStream);
                _bufferedStream.Position = 0;
                _isBuffered = true;
            }
        }

        public override bool CanRead => _innerStream.CanRead || _isBuffered;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _bufferedStream.Length;

        public override long Position
        {
            get => _bufferedStream.Position;
            set => _bufferedStream.Position = value;
        }

        public override void Flush() => _innerStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (!_isBuffered) {
                // If not buffered yet, buffer it synchronously (should ideally be async)
                // For simplicity in this sync method, we'll buffer on first read if not already.
                // In a real-world scenario, you'd ensure buffering happens async before sync reads.
                BufferStreamAsync().GetAwaiter().GetResult(); 
            }
            return _bufferedStream.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await BufferStreamAsync();
            return await _bufferedStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            // Ensure stream is buffered before seeking
            BufferStreamAsync().GetAwaiter().GetResult(); // Synchronous wait for buffering
            return _bufferedStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _bufferedStream.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}