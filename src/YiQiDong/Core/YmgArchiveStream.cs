using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using YiQiDong.Utils;

namespace YiQiDong.Core
{
    public class YmgArchiveStream : Stream
    {
        private bool isReadBegin = true;
        private Stream baseStream;
        public override bool CanRead => baseStream.CanRead;
        public override bool CanSeek => baseStream.CanSeek;
        public override bool CanWrite => baseStream.CanWrite;
        public override long Length => baseStream.Length;

        public YmgArchiveStream(Stream baseStream)
        {
            this.baseStream = baseStream;
        }

        //读取头部数据
        private int ReadHead(byte[] buffer, int offset, int count)
        {
            var ret = baseStream.Read(buffer, offset, count);
            //读YMG文件头
            var fileHead = Encoding.ASCII.GetString(buffer, offset, 2);
            //写回压缩文件原始文件头
            var srcFileHead = YmgFileUtils.GetSrcFileHead(fileHead);
            Encoding.ASCII.GetBytes(srcFileHead, new Span<byte>(buffer, offset, count));

            if (ret > 0)
                isReadBegin = false;

            //返回读取的字节数量
            return ret;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (isReadBegin)
                return ReadHead(buffer, offset, count);
            return baseStream.Read(buffer, offset, count);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (isReadBegin)
                return Task.FromResult(ReadHead(buffer, offset, count));
            return baseStream.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override long Position
        {
            get => baseStream.Position;
            set
            {
                baseStream.Position = value;
                isReadBegin = value == 0;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var newPosition = baseStream.Seek(offset, origin);
            isReadBegin = newPosition == 0;
            return newPosition;
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            baseStream.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            baseStream.Flush();
        }
    }
}
