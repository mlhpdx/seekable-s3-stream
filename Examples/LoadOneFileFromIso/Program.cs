using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using DiscUtils.Iso9660;

namespace SeekableS3Stream.Examples.LoadOneFileFromIso
{
    class Program
    {
        const string BUCKET = "rds.nsrl.nist.gov";
        const string KEY = "RDS/current/RDS_ios.iso"; // "RDS/current/RDS_modern.iso";
        const string FILENAME = "READ_ME.TXT";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();

            using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 1 * 1024 * 1024, 4);
            using var iso = new CDReader(stream, true);
            using var file = iso.OpenFile(FILENAME, FileMode.Open, FileAccess.Read);
            using var reader = new StreamReader(file);
            var content = await reader.ReadToEndAsync();

            await Console.Out.WriteLineAsync($"{stream.TotalRead:0,000} read {stream.TotalLoaded:0,000} loaded of {stream.Length:0,000} bytes");
        }
    }
}
