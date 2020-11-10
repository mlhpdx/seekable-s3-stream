# Seekable S3 Stream

Some files are big enough that working on them in memory isn't possible, or desirable. Even if speed isn't an issue, the data transfer cost of moving the file from S3 is thousands of times more expensive than solving the core issue and using the S3 API's power to implement optimized reads. This repo contains an demonstration of implementing such a stream in C#.

For the full explaination, check out the article on [Medium](https://medium.com/@lee.harding).