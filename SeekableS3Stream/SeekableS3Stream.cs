using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace Cppl.Utilities.AWS
{
    public class SeekableS3Stream : Stream
    {
        const long DEFAULT_PAGE_LENGTH = 25 * 1024 * 1024;
        const int DEFAULT_MAX_PAGE_COUNT = 20;

        internal class MetaData
        {
            public IAmazonS3 S3;
            
            public string Bucket;
            public string Key;

            public string S3eTag;

            public long Length = 0;

            public long PageSize = DEFAULT_PAGE_LENGTH;
            public long MaxPages = DEFAULT_MAX_PAGE_COUNT;

            public ConcurrentDictionary<long, byte[]> Pages;
            public ConcurrentDictionary<long, long> HotList;
        }

        MetaData _metadata = null;
        long _position = 0;

        public long TotalRead { get; private set; }
        public long TotalLoaded { get; private set; }

        public SeekableS3Stream(IAmazonS3 s3, string bucket, string key, long page = DEFAULT_PAGE_LENGTH, int maxpages = DEFAULT_MAX_PAGE_COUNT)
        {
            _metadata = new MetaData() {
                S3 = s3,
                Bucket = bucket,
                Key = key,
                PageSize = page,
                MaxPages = maxpages,
                Pages = new ConcurrentDictionary<long, byte[]>(Environment.ProcessorCount, maxpages),
                HotList = new ConcurrentDictionary<long, long>(Environment.ProcessorCount, maxpages)
            };

            var m = _metadata.S3.GetObjectMetadataAsync(_metadata.Bucket, _metadata.Key).GetAwaiter().GetResult();
            _metadata.Length = m.ContentLength;
            _metadata.S3eTag = m.ETag;
        }

        private SeekableS3Stream() { }

        public SeekableS3Stream Fork() 
        {
            var s5 = new SeekableS3Stream();
            s5._metadata = _metadata;
            s5._position = _position;
            return s5;
        }

        protected override void Dispose(bool disposing) => base.Dispose(disposing);

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;

        public override long Length => _metadata.Length;
        public override long Position
        {
            get => _position;
            set => Seek(value, value >= 0 ? SeekOrigin.Begin : SeekOrigin.End);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (_position < 0 || _position >= Length)
                return 0;

            long p = _position;
            do
            {
                long i = p / _metadata.PageSize;
                long o = p % _metadata.PageSize;

                _metadata.Pages.TryGetValue(i, out var b);

                if (b == null)
                {
                    // if we have too many pages, drop the coolest
                    while (_metadata.Pages.Count >= _metadata.MaxPages)
                    {
                        var trim = _metadata.Pages.OrderBy(kv => _metadata.HotList[kv.Key]).First().Key;
                        _metadata.Pages.TryRemove(trim, out var removed);
                    }

                    long s = i * _metadata.PageSize;
                    long e = s + Math.Min(_metadata.PageSize, _metadata.Length - s); // read in a single page (we're looping)

                    var go = new GetObjectRequest()
                    {
                        BucketName = _metadata.Bucket,
                        Key = _metadata.Key,
                        EtagToMatch = _metadata.S3eTag, // ensure the object hasn't change under us
                        ByteRange = new ByteRange(s, e)
                    };

                    b = new byte[e - s];
                    _metadata.Pages.AddOrUpdate(i, i => b, (i, _) => b);

                    int read = 0;
                    using (var r = await _metadata.S3.GetObjectAsync(go, cancellationToken))
                    {
                        do
                        {
                            read += r.ResponseStream.Read(b, read, b.Length - read);
                        } while (read < b.Length);
                    }
                    TotalLoaded += read;
                }
                _metadata.HotList.AddOrUpdate(i, i => 1, (i, c) => c + 1);

                long l = Math.Min(b.Length - o, count);
                Array.Copy(b, (int)o, buffer, offset, (int)l);
                offset += (int)l;
                count -= (int)l;
                p += (int)l;
            } while (count > 0 && p < _metadata.Length);

            long c = p - _position;
            TotalRead += c;
            _position = p;
            return (int)c;
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            ReadAsync(buffer, offset, count, default(CancellationToken)).GetAwaiter().GetResult();

        public override long Seek(long offset, SeekOrigin origin)
        {
            var newpos = _position;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    newpos = offset; // offset must be positive
                    break;
                case SeekOrigin.Current:
                    newpos += offset; // + or -
                    break;
                case SeekOrigin.End:
                    newpos = _metadata.Length - Math.Abs(offset); // offset must be negative?
                    break;
            }
            if (newpos < 0 || newpos > _metadata.Length) 
                throw new InvalidOperationException("Stream position is invalid.");
            return _position = newpos;
        }

        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override void Flush() => throw new NotImplementedException();
    }
}
