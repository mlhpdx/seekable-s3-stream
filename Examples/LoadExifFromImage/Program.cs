using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using MetadataExtractor;

namespace SeekableS3Stream.Examples.LoadExifFromImage
{
    // Read image metadata (EXIF, etc.) from 10 images stored in S3 usng unmodified MetadataExtractor.  
    // In this case the visual content of the files, which is the vast majority of their size, 
    // isn't needed. 
    class Program
    {
        const string BUCKET = "ladi";
        const string PREFIX = "Images/";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();
            var images = await s3.ListObjectsAsync(BUCKET, PREFIX);
            long size = 0L;
            long loaded = 0L;
            long read = 0L; 
            foreach (var image in images.S3Objects.Where(o => o.Key.EndsWith(".jpg")).Take(10)) {
                size += image.Size;
                using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, image.Key, 16 * 1024, 8);
                var directories = ImageMetadataReader.ReadMetadata(stream);
                read = stream.TotalRead;
                loaded += stream.TotalLoaded;
            }
            // loads less than 1% of the image file content
            await Console.Out.WriteLineAsync($"{read:0,000} read {loaded:0,000} loaded of {size:0,000} bytes");
        }
    }
}