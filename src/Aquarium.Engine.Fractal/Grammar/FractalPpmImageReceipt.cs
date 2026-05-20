namespace Aquarium.Engine.Fractal.Grammar;

public readonly record struct FractalPpmImageReceipt(
    int Width,
    int Height,
    int MaxChannelValue,
    int PixelCount,
    int NonBlackPixelCount,
    ulong RgbChecksum,
    ulong LuminanceChecksum);

public static class FractalPpmImageReceiptBuilder
{
    public static FractalPpmImageReceipt Build(ReadOnlySpan<byte> bytes)
    {
        var cursor = 0;
        var magic = ReadToken(bytes, ref cursor);
        if (!magic.SequenceEqual("P6"u8))
        {
            throw new FormatException("Only binary P6 PPM reference renders are supported.");
        }

        var width = ParsePositiveInt(ReadToken(bytes, ref cursor), "width");
        var height = ParsePositiveInt(ReadToken(bytes, ref cursor), "height");
        var maxChannelValue = ParsePositiveInt(ReadToken(bytes, ref cursor), "max channel value");
        if (maxChannelValue > 255)
        {
            throw new FormatException("Only 8-bit PPM reference renders are supported.");
        }

        ConsumeSingleWhitespace(bytes, ref cursor);
        var expectedBytes = checked(width * height * 3);
        if (bytes.Length - cursor < expectedBytes)
        {
            throw new FormatException("PPM reference render ended before all RGB pixels were present.");
        }

        var rgbChecksum = 1469598103934665603UL;
        var luminanceChecksum = 1469598103934665603UL;
        var nonBlack = 0;
        var pixels = bytes.Slice(cursor, expectedBytes);
        for (var index = 0; index < pixels.Length; index += 3)
        {
            var r = pixels[index];
            var g = pixels[index + 1];
            var b = pixels[index + 2];
            rgbChecksum = Step(rgbChecksum, r);
            rgbChecksum = Step(rgbChecksum, g);
            rgbChecksum = Step(rgbChecksum, b);
            var luminance = (byte)(((r * 54) + (g * 183) + (b * 19)) >> 8);
            luminanceChecksum = Step(luminanceChecksum, luminance);
            if ((r | g | b) != 0)
            {
                nonBlack++;
            }
        }

        return new FractalPpmImageReceipt(
            width,
            height,
            maxChannelValue,
            width * height,
            nonBlack,
            rgbChecksum,
            luminanceChecksum);
    }

    private static ulong Step(ulong hash, byte value)
    {
        hash ^= value;
        hash *= 1099511628211UL;
        return hash;
    }

    private static ReadOnlySpan<byte> ReadToken(ReadOnlySpan<byte> bytes, ref int cursor)
    {
        SkipWhitespaceAndComments(bytes, ref cursor);
        var start = cursor;
        while (cursor < bytes.Length && !IsWhitespace(bytes[cursor]) && bytes[cursor] != (byte)'#')
        {
            cursor++;
        }

        if (cursor == start)
        {
            throw new FormatException("PPM reference render header ended unexpectedly.");
        }

        return bytes[start..cursor];
    }

    private static void SkipWhitespaceAndComments(ReadOnlySpan<byte> bytes, ref int cursor)
    {
        while (cursor < bytes.Length)
        {
            if (IsWhitespace(bytes[cursor]))
            {
                cursor++;
                continue;
            }

            if (bytes[cursor] != (byte)'#')
            {
                return;
            }

            while (cursor < bytes.Length && bytes[cursor] != (byte)'\n')
            {
                cursor++;
            }
        }
    }

    private static void ConsumeSingleWhitespace(ReadOnlySpan<byte> bytes, ref int cursor)
    {
        if (cursor >= bytes.Length || !IsWhitespace(bytes[cursor]))
        {
            throw new FormatException("PPM reference render header is missing the pixel-data separator.");
        }

        cursor++;
    }

    private static bool IsWhitespace(byte value)
    {
        return value is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n';
    }

    private static int ParsePositiveInt(ReadOnlySpan<byte> token, string name)
    {
        var value = 0;
        foreach (var digit in token)
        {
            if (digit is < (byte)'0' or > (byte)'9')
            {
                throw new FormatException($"PPM {name} is not an integer.");
            }

            value = checked((value * 10) + digit - (byte)'0');
        }

        if (value <= 0)
        {
            throw new FormatException($"PPM {name} must be positive.");
        }

        return value;
    }
}
