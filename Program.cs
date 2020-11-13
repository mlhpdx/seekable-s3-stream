using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Parquet;
using Parquet.Data;

namespace Seekable_S3_Stream
{
    class Program
    {
        const string BUCKET = "ursa-labs-taxi-data";
        const string KEY = "2019/06/data.parquet";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();

            using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 1 * 1024 * 1024, 4);
            using var parquet = new ParquetReader(stream);
            var fields = parquet.Schema.GetDataFields();

            await Console.Out.WriteLineAsync($"{stream.TotalRead / (float)stream.Length * 100}% read, {stream.TotalLoaded / (float)stream.Length * 100}% loaded");
        }
    }
}
