using System.Buffers;
using System.Text.Json;
using System.Threading.Channels;
using System.IO.Pipelines;

namespace Mycoverse.Net.Json;

// Copied from https://stackoverflow.com/a/58621460 because this is way more work than it should be.
// Slightly modified to remove warnings and accept JsonSerializerOptions.
public class JsonStream
{

    private const byte NL = (byte)'\n';
    private const int MaxStackLength = 128;

    private static SequencePosition ReadItems<T>(ChannelWriter<T> writer, in ReadOnlySequence<byte> sequence,
                              bool isCompleted, JsonSerializerOptions opts, CancellationToken token)
    {
        var reader = new SequenceReader<byte>(sequence);

        while (!reader.End && !token.IsCancellationRequested) // loop until we've read the entire sequence
        {
            if (reader.TryReadTo(out ReadOnlySpan<byte> itemBytes, NL, advancePastDelimiter: true)) // we have an item to handle
            {
                var item = JsonSerializer.Deserialize<T>(itemBytes, opts);
                writer.TryWrite(item!);
            }
            else if (isCompleted) // read last item which has no final delimiter
            {
                var item = ReadLastItem<T>(sequence.Slice(reader.Position), opts);
                writer.TryWrite(item);
                reader.Advance(sequence.Length); // advance reader to the end
            }
            else break; // no more items in this sequence
        }

        return reader.Position;
    }

    private static T ReadLastItem<T>(in ReadOnlySequence<byte> sequence, JsonSerializerOptions opts)
    {
        var length = (int)sequence.Length;

        if (length < MaxStackLength) // if the item is small enough we'll stack allocate the buffer
        {
            Span<byte> byteBuffer = stackalloc byte[length];
            sequence.CopyTo(byteBuffer);
            var item = JsonSerializer.Deserialize<T>(byteBuffer);
            return item!;
        }
        else // otherwise we'll rent an array to use as the buffer
        {
            var byteBuffer = ArrayPool<byte>.Shared.Rent(length);

            try
            {
                sequence.CopyTo(byteBuffer);
                var item = JsonSerializer.Deserialize<T>(byteBuffer, opts);
                return item!;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(byteBuffer);
            }

        }
    }

    public static ChannelReader<T> DeserializeToChannel<T>(Stream stream, JsonSerializerOptions opts, CancellationToken token)
    {
        var pipeReader = PipeReader.Create(stream);
        var channel = Channel.CreateUnbounded<T>();
        var writer = channel.Writer;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var result = await pipeReader.ReadAsync(token); // read from the pipe

                var buffer = result.Buffer;

                var position = ReadItems(writer, buffer, result.IsCompleted, opts, token); // read complete items from the current buffer

                if (result.IsCompleted)
                    break; // exit if we've read everything from the pipe

                pipeReader.AdvanceTo(position, buffer.End); //advance our position in the pipe
            }

            pipeReader.Complete();
        }, token)
        .ContinueWith(t =>
        {
            pipeReader.Complete();
            writer.TryComplete(t.Exception);
        });

        return channel.Reader;
    }
}
