using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Parquet;
using Parquet.Data;

namespace SeekableS3Stream.Examples.LoadSchemaFromParquet
{
    class Program
    {
        const string BUCKET = "ursa-labs-taxi-data";
        const string KEY = "2019/06/data.parquet";
        static async Task Main(string[] args)
        {
            var s3 = new AmazonS3Client();

            using var stream = new Cppl.Utilities.AWS.SeekableS3Stream(s3, BUCKET, KEY, 1 * 1024 * 1024, 4);
            using var parquet = await ParquetReader.CreateAsync(stream);
            var fields = parquet.Schema.GetDataFields();

            await Console.Out.WriteLineAsync($"{stream.TotalRead:0,000} read {stream.TotalLoaded:0,000} loaded of {stream.Length:0,000} bytes");
        }
    }
}
