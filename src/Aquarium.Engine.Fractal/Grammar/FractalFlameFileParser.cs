using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace Aquarium.Engine.Fractal.Grammar;

public static class FractalFlameFileParser
{
    public static FractalFlameDefinition ParseFirst(string source, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(source);

        var document = XDocument.Parse(source);
        var flame = document.Descendants("flame").FirstOrDefault()
            ?? throw new FormatException("Flame file does not contain a flame element.");
        var name = ((string?)flame.Attribute("name")) ?? "flame";
        var transforms = flame.Elements("xform").Select((xform, index) => ParseXform(xform, index)).ToArray();
        return new FractalFlameDefinition(name, Vector2.Zero, seed, transforms);
    }

    private static FractalFlameTransform2D ParseXform(XElement element, int index)
    {
        var coefs = ParseFloatList((string?)element.Attribute("coefs"), expectedCount: 6, "xform coefs");
        return new FractalFlameTransform2D(
            ((string?)element.Attribute("name")) ?? $"xform/{index:0000}",
            new Vector4(coefs[0], coefs[1], coefs[3], coefs[4]),
            new Vector2(coefs[2], coefs[5]),
            ParseOptionalFloat(element, "weight", 1.0f),
            ParseOptionalFloat(element, "color", 0.0f),
            new FractalFlameVariationWeights(
                ParseOptionalFloat(element, "linear", 0.0f),
                ParseOptionalFloat(element, "spherical", 0.0f),
                ParseOptionalFloat(element, "bubble", 0.0f)));
    }

    private static float ParseOptionalFloat(XElement element, string name, float fallback)
    {
        var attribute = (string?)element.Attribute(name);
        return attribute is null ? fallback : float.Parse(attribute, CultureInfo.InvariantCulture);
    }

    private static float[] ParseFloatList(string? source, int expectedCount, string label)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new FormatException($"{label} must not be empty.");
        }

        var values = source
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => float.Parse(value, CultureInfo.InvariantCulture))
            .ToArray();
        if (values.Length != expectedCount)
        {
            throw new FormatException($"{label} expected {expectedCount} values, got {values.Length}.");
        }

        return values;
    }
}
