# Seekable S3 Stream

Some S3 objects are big enough that working on them in memory isn't desirable, or even possible. This library performs selective, efficient data transfer from S3 that is orders of magnitude faster and more efficient than naiively using `MemoryStream` while maintaining compatibility with libraries and packages that work with a `Stream` interface.  Examples for reading ISO, Zip, JPG and Parquet files are included in the repo.

For the full explaination, check out the article on [Medium](https://medium.com/circuitpeople/random-access-seekable-streams-for-amazon-s3-in-c-bd2414255dcd).
