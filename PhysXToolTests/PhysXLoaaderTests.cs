using PhysXTool;

namespace Tests;

public sealed class PhysXLoaderTests
{
    [Fact]
    public static void Load_ReadOnlySpan_EmptyContent_DoesNotModifyState()
    {
        var loader = new PhysXLoader();
        ReadOnlySpan<char> emptySpan = ReadOnlySpan<char>.Empty;
        loader.Load(emptySpan);
        Assert.True(loader.Collection.IsEmpty);
        Assert.Empty(loader.Materials);
        Assert.Empty(loader.Actors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public static void Load_String_NullOrEmptyContent_DoesNotModifyState(string? emptyContent)
    {
        var loader = new PhysXLoader();
        loader.Load(emptyContent!);
        Assert.True(loader.Collection.IsEmpty);
        Assert.Empty(loader.Materials);
        Assert.Empty(loader.Actors);
    }

    [Fact]
    public static void Load_Stream_NullStream_ThrowsArgumentNullException_WithoutClosureAllocation()
    {
        Assert.Throws<ArgumentNullException>("xmlContent", static () =>
        {
            var loader = new PhysXLoader();
            loader.Load((Stream)null!);
        });
    }

    [Fact]
    public static void Load_Stream_UnreadableStream_DoesNotModifyState()
    {
        var loader = new PhysXLoader();
        using var unreadableStream = new MockUnreadableStream();
        loader.Load(unreadableStream);
        Assert.True(loader.Collection.IsEmpty);
        Assert.Empty(loader.Materials);
        Assert.Empty(loader.Actors);
    }

    [Fact]
    public static void Load_Stream_EmptySeekableStream_DoesNotModifyState()
    {
        var loader = new PhysXLoader();
        using var emptyStream = new MemoryStream();
        loader.Load(emptyStream);
        Assert.True(loader.Collection.IsEmpty);
        Assert.Empty(loader.Materials);
        Assert.Empty(loader.Actors);
    }
    private sealed class MockUnreadableStream : Stream
    {
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position 
        { 
            get => throw new NotSupportedException(); 
            set => throw new NotSupportedException(); 
        }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}