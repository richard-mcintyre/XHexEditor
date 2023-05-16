namespace XHexEditor.Providers
{
    public interface IProvider : IDisposable
    {
        long Length { get; }

        bool IsModified { get; }

        /// <summary>
        /// Inserts a byte at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the specified value will be inserted</param>
        /// <param name="value">Value to insert</param>
        void InsertByte(long atIndex, byte value);

        /// <summary>
        /// Deletes a byte at the specified index
        /// </summary>
        void Delete(long atIndex);

        /// <summary>
        /// Modfies the value of a byte at the specified index
        /// </summary>
        void Modify(long atIndex, byte value);

        /// <summary>
        /// Changes the bytes to the specified bytes at the specified index
        /// </summary>
        /// <param name="atIndex">Index at which the bytes will be modified</param>
        /// <param name="value">Value to update</param>
        void Modify(long atIndex, byte[] buffer, int offset, int count);

        /// <summary>
        /// Copies the bytes in this provider to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this provider</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        int CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count);

        /// <summary>
        /// Copies the bytes in this provider to the specified buffer
        /// </summary>
        /// <param name="sourceIndex">Starting byte index to copy in this provider</param>
        /// <param name="destBuffer">Buffer to copy bytes into</param>
        /// <param name="destIndex">Index to copy bytes to in the destination buffer</param>
        /// <param name="count">Number of bytes to copy</param>
        /// <param name="modifications">Information about changes that have been made to the requested byte range</param>
        int CopyTo(long sourceIndex, byte[] destBuffer, int destIndex, int count, ProviderModifications? modifications);

        /// <summary>
        /// Gets a stream representing this provider include any changes to bytes
        /// </summary>
        /// <returns></returns>
        Stream AsStream();

        /// <summary>
        /// Applies any changes made to this provider to the underlying stream
        /// </summary>
        Task ApplyChangesAsync(CancellationToken cancellation, IProgress<ApplyChangesProgressInfo>? progress);

        /// <summary>
        /// Fired when a byte has been deleted
        /// </summary>
        event EventHandler<ProviderByteEventArgs> ByteDeleted;

        /// <summary>
        /// Fired when a byte has been inserted
        /// </summary>
        event EventHandler<ProviderByteEventArgs> ByteInserted;
    }
}