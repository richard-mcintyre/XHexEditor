using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    /// <summary>
    /// Exposes an IProvider as a stream
    /// </summary>
    class ProviderStream : Stream
    {
        #region Construction

        public ProviderStream(IProvider provider)
        {
            _provider = provider;
        }

        #endregion

        #region Fields

        private readonly IProvider _provider;
        private long _position;

        #endregion

        #region Properties

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _provider.Length;

        public override long Position
        {
            get => _position;
            set => _position = value;
        }

        #endregion

        #region Methods

        public override void Flush()
        { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = offset;
                    break;
                case SeekOrigin.Current:
                    _position += offset;
                    break;
                case SeekOrigin.End:
                    _position = _provider.Length - offset;
                    break;
            }

            return _position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _provider.CopyTo(_position, buffer, offset, count);
            _position += read;

            return read;
        }

        public override void SetLength(long value) =>
            throw new NotImplementedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotImplementedException();

        #endregion
    }

}
