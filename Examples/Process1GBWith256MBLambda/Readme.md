# Impossible Lambda

This example demonstrates a Lambda function that uses `SeekableS3Stream` to fully process a file that is much larger than the available RAM.  In this case the Lambda is intentionally under-provisioned with ony 256 MB of RAM while the file to be processed is nearly 500MB and decompresses to around 1GB of content.  Processing such a file with a naiive solution where it is read entirely into RAM (likely using a `MemoryStream`) would obviously not work here, so this is a case where `SeekableS3Stream` makes the "impossible" possible. 

**NOTE**: Running this Lambda processes a very large amount of data, and will cost you money.  Keep that in mind, please.

Beyond unblocking a real use case, the seekable-stream approach shows additional perfomance benefits. The time between starting the processing and the first line being read is around 1 second -- far less than copying the file alone takes with a "read it all" to memory approach.  Additionally, with that head-start the overall run time is also substantially less (all other things equal). Nice.

## Deploying from the Command Line

This application may be deployed using the [Amazon.Lambda.Tools Global Tool](https://github.com/aws/aws-extensions-for-dotnet-cli#aws-lambda-amazonlambdatools) from the command line.

Install Amazon.Lambda.Tools Global Tools if not already installed.
```
    dotnet tool install -g Amazon.Lambda.Tools
```

If already installed check if new version is available.
```
    dotnet tool update -g Amazon.Lambda.Tools
```

Deploy application (run from the project directory)
```
    dotnet lambda deploy-serverless
```

## Results

Here are the log messages and output of a run of the function. In two minutes and eleven seconds, the 9 million lines in the zip file (1.2 GB of text) are read while only 256MB of RAM is used.

```
2020-11-15T21:10:29.717-08:00	START RequestId: 14e3c267-6782-4943-a619-a45c859e7593 Version: $LATEST
2020-11-15T21:10:29.851-08:00	00:00:00.0000017: Getting started.
2020-11-15T21:10:32.554-08:00	00:00:02.7031771: First row received.
2020-11-15T21:12:41.791-08:00	00:02:11.9228511: Done reading rows.
2020-11-15T21:12:41.791-08:00	00:02:11.9231731: 394,072,280 read 415,236,096 loaded of 407,250,944 bytes
2020-11-15T21:12:41.853-08:00	END RequestId: 14e3c267-6782-4943-a619-a45c859e7593
2020-11-15T21:12:41.853-08:00	REPORT RequestId: 14e3c267-6782-4943-a619-a45c859e7593 Duration: 132133.06 ms Billed Duration: 132200 ms Memory Size: 256 MB Max Memory Used: 256 MB
```

```json
{
  "IsoPath": "s3://rds.nsrl.nist.gov/RDS/current/RDS_ios.iso",
  "TotalRead": 394072280,
  "TotalLoaded": 415236096,
  "Length": 1202942018,
  "TotalRows": 9037375,
  "Status": "ok"
}
```
