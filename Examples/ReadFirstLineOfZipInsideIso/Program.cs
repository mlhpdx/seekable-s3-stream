using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Amazon.S3;
using DiscUtils.Iso9660;

namespace SeekableS3Stream.Examples.ReadFirstLineOfZipInsideIso
{
    // read the first line of text from a CSV file embedded in a zip file contained in an ISO
    // disk image file where the ISO is stored on S3 and minimal bytes are actually
    // transferred to the client (this code).
    class Program
    {
        const string BUCKET = "rds.nsrl.nist.gov";
        const string KEY = "RDS/current/RDS_modern.iso";
        const string ZIPNAME = "NSRLFILE.ZIP";
        const string FILENAME = "NSRLFile.txt";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();

            using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 128 * 1024, 12);
            using var iso = new CDReader(stream, true);
            using var embedded = iso.OpenFile(ZIPNAME, FileMode.Open, FileAccess.Read);
            using var zip = new ZipArchive(embedded);
            using var file = zip.GetEntry(FILENAME).Open();
            using var reader = new StreamReader(file);
            await reader.ReadLineAsync();
            
            await Console.Out.WriteLineAsync($"{stream.TotalRead:0,000} read {stream.TotalLoaded:0,000} loaded of {stream.Length:0,000} bytes");
        }
    }
}
