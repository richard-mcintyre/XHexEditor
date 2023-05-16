using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XHexEditor.Providers
{
    public class StreamProvider : IProvider
    {
        #region Construction

        public StreamProvider(Stream stream)
            : this(stream, false)
        {
        }

        public StreamProvider(Stream stream, bool leaveOpen)
        {
            _stream = stream;
            _leaveStreamOpen = leaveOpen;

            _table = new Table(SegmentFactory.Create(stream, 0, stream.Length));
        }

        ~StreamProvider()
        {
            Dispose(false);
        }

        #endregion

        #region Fields

        private readonly Stream _stream;
        private readonly bool _leaveStreamOpen;

        private Table _table;
        private bool _isModified;
        private byte[] _applyChangesBuffer;

        #endregion

        #region Properties

        public long Length => _table.GetLength();

        public bool IsModified => _isModified;

        #endregion

        #region Methods

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_leaveStreamOpen == false)
                {
                    _stream?.Dispose();
                }
            }
        }

        public void InsertByte(long atIndex, byte value)
        {
            _table.Insert(atIndex, value);
            _isModified = true;
            OnByteInserted(atIndex);
        }

        public void Delete(long atIndex)
        {
            _table.Delete(atIndex);
            _isModified = true;
            OnByteDeleted(atIndex);
        }

        public void Modify(long atIndex, byte value)
        {
            _table.Modify(atIndex, value);
            _isModified = true;
        }

        /// <summary>
        /// Changes the bytes to the specified bytes at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the bytes will be modified</param>
        /// <param name="value">Value to update</param>
        public void Modify(long atIndex, byte[] buffer, int offset, int count) =>
            _table.Modify(atIndex, buffer, offset, count);

        public int CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count) =>
            CopyTo(sourceIndex, destBuffer, destIndex, count, null);

        public int CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications)
        {
            count = (int)Math.Min(count, _table.GetLength() - sourceIndex);

            _table.CopyTo(sourceIndex, destBuffer, destIndex, count, modifications);

            return count;
        }

        public Stream AsStream() => new ProviderStream(this);

        public async Task ApplyChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress)
        {
            // Write the contents to another file, then copy that contents to the underlying stream
            long finalLength = this.Length;
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew(null, finalLength))
            {
                using (MemoryMappedViewStream tempStream = mmf.CreateViewStream())
                {
                    using (Stream thisStream = AsStream())
                    {
                        progress?.Report(new ApplyChangesProgressInfo(ApplyChangesStatus.Preparing, 0));
                        await thisStream.CopyToAsync(tempStream, cancellation).ConfigureAwait(false);
                    }

                    // Now copy it to the stream in this provider
                    if (_stream.Length != finalLength)
                        _stream.SetLength(finalLength);

                    _stream.Position = 0;
                    tempStream.Position = 0;

                    // Cannot use Stream.Copy as tempStream will be the OS page size (4k for 32bit?)
                    //tempStream.CopyTo(_stream);
                    //byte[] buffer = new byte[Math.Min(finalLength, 64000)];
                    if (_applyChangesBuffer is null)
                        _applyChangesBuffer = new byte[1024 * 1024];

                    long leftToCopy = finalLength;

                    while (leftToCopy > 0)
                    {
                        progress?.Report(new ApplyChangesProgressInfo(ApplyChangesStatus.Applying, leftToCopy));

                        // read will be at least the OS page size
                        int read = tempStream.Read(_applyChangesBuffer, 0, _applyChangesBuffer.Length);
                        if (read > leftToCopy)
                            read = (int)leftToCopy;

                        leftToCopy -= read;

                        _stream.Write(_applyChangesBuffer, 0, read);
                    }

                    // Now that the stream has been updated, we only need a single segment in our table
                    _table = new Table(SegmentFactory.Create(_stream, 0, _stream.Length));
                }
            }

            _isModified = false;
        }

        #endregion

        #region Events

        public event EventHandler<ProviderByteEventArgs>? ByteDeleted;
        private void OnByteDeleted(long atIndex) =>
            this.ByteDeleted?.Invoke(this, new ProviderByteEventArgs() { AtIndex = atIndex });

        public event EventHandler<ProviderByteEventArgs>? ByteInserted;
        private void OnByteInserted(long atIndex) =>
            this.ByteInserted?.Invoke(this, new ProviderByteEventArgs() { AtIndex = atIndex });

        #endregion
    }
}
