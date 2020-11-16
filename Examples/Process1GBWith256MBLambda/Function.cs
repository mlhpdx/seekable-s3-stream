using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Text.Json;
using System.Diagnostics;
using Amazon.Lambda.Core;
using Amazon.S3;
using DiscUtils.Iso9660;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Process1GBWith256MBLambda
{
    public class Functions
    {
        const string BUCKET = "rds.nsrl.nist.gov";
        const string KEY = "RDS/current/RDS_ios.iso";
        const string ZIPNAME = "NSRLFILE.ZIP";
        const string FILENAME = "NSRLFile.txt";

        public Functions()
        {
        }

        public async Task<object> Get(JsonDocument request, ILambdaContext context)
        {
            var s3 = new AmazonS3Client();

            // easier than doing math on the timestamps in logs
            var timer = new Stopwatch();
            timer.Start();

            context.Logger.LogLine($"{timer.Elapsed}: Getting started.");
            using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 12 * 1024 * 1024, 5);
            using var iso = new CDReader(stream, true);
            using var embedded = iso.OpenFile(ZIPNAME, FileMode.Open, FileAccess.Read);
            using var zip = new ZipArchive(embedded, ZipArchiveMode.Read);
            var entry = zip.GetEntry(FILENAME);
            using var file = entry.Open();
            using var reader = new StreamReader(file);

            // how soon do we get the first line?
            var line = await reader.ReadLineAsync();
            context.Logger.LogLine($"{timer.Elapsed}: First row received.");

            // read all of the remainline lines (it'll take a while...)
            ulong rows = 1;
            while ((line  = await reader.ReadLineAsync()) != null) {
                ++rows;
            }
            context.Logger.LogLine($"{timer.Elapsed}: Done reading rows.");
            
            // the total amount read should be close to the total file size, but the amount loaded may be greated than
            // the file size if too few ranges are held in the MRU and end-up being loaded multiple times.
            context.Logger.LogLine($"{timer.Elapsed}: {stream.TotalRead:0,000} read {stream.TotalLoaded:0,000} loaded of {stream.Length:0,000} bytes");
            timer.Stop();

            return new { 
                IsoPath = $"s3://{BUCKET}/{KEY}", 
                stream.TotalRead,
                stream.TotalLoaded,
                entry.Length,
                TotalRows = rows,
                Status = "ok"
            };
        }
    }
}
