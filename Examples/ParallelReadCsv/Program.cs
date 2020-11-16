using System;
using System.Threading.Tasks;
using Amazon.S3;

namespace SeekableS3Stream.Examples.ParallelReadCsv
{
    // 
    class Program
    {
        const string BUCKET = "irs-form-990";
        const string KEY = "index_2019.csv";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();

            using var stream1 = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 1024 * 1024, 100);

            // look for the first line ending past the middle of the file (only reads one range)
            stream1.Position = stream1.Length / 2;
            while (stream1.ReadByte() != '\n');

            // fork stream1 to get another stream that shares range caching (but has a separate position) 
            var split = stream1.Position;
            using var stream2 = stream1.Fork();

            ulong count1() {
                Console.Out.WriteLine($"{DateTime.Now}: Starting count1.");
                stream1.Position = 0;
                var c = 0UL;
                while (stream1.Position < split)
                    if (stream1.ReadByte() == '\n') 
                        c++;
                Console.Out.WriteLine($"{DateTime.Now}: Finished count1.");
                return c;
            }   
            ulong count2() {
                Console.Out.WriteLine($"{DateTime.Now}: Starting count2.");
                var c = 0UL;
                while (stream2.Position < stream2.Length)
                    if (stream2.ReadByte() == '\n') 
                        c++;
                Console.Out.WriteLine($"{DateTime.Now}: Finished count2.");
                return c;
            }

            // run counting of the two streams in parallel
            var counts = await Task.WhenAll(new[] { Task.Run(count1), Task.Run(count2) });

            await Console.Out.WriteLineAsync($"#1: {stream1.TotalRead:0,000} read {stream1.TotalLoaded:0,000} loaded of {stream1.Length:0,000} bytes");
            await Console.Out.WriteLineAsync($"#2: {stream2.TotalRead:0,000} read {stream2.TotalLoaded:0,000} loaded of {stream2.Length:0,000} bytes");
        }
    }
}
