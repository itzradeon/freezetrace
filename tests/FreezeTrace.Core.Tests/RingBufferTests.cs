using FreezeTrace.Core.Buffers;

namespace FreezeTrace.Core.Tests;

public sealed class RingBufferTests
{
    [Fact]
    public void Snapshot_ReturnsItemsInInsertionOrder()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);

        Assert.Equal(new[] { 1, 2 }, buffer.Snapshot());
    }

    [Fact]
    public void Add_OverwritesOldestItemWhenFull()
    {
        var buffer = new RingBuffer<int>(3);

        buffer.Add(1);
        buffer.Add(2);
        buffer.Add(3);
        buffer.Add(4);

        Assert.Equal(new[] { 2, 3, 4 }, buffer.Snapshot());
    }

    [Fact]
    public void Constructor_RejectsInvalidCapacity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RingBuffer<int>(0));
    }
}
