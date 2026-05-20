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
        var isJWildfireDialect = string.Equals(document.Root?.Name.LocalName, "Flames", StringComparison.Ordinal)
            || flame.Elements("xform").Any(xform => xform.Element("variationGroup") is not null);
        var transforms = flame.Elements("xform").Select((xform, index) => ParseXform(xform, index, isJWildfireDialect)).ToArray();
        return new FractalFlameDefinition(name, Vector2.Zero, seed, transforms);
    }

    private static FractalFlameTransform2D ParseXform(XElement element, int index, bool isJWildfireDialect)
    {
        var coefs = ParseFloatList((string?)element.Attribute("coefs"), expectedCount: 6, "xform coefs");
        var variationGroup = element.Element("variationGroup");
        var variationSource = variationGroup ?? element;
        var matrix = isJWildfireDialect
            ? new Vector4(coefs[0], coefs[2], coefs[1], coefs[3])
            : new Vector4(coefs[0], coefs[1], coefs[3], coefs[4]);
        var translation = isJWildfireDialect
            ? new Vector2(coefs[4], coefs[5])
            : new Vector2(coefs[2], coefs[5]);

        return new FractalFlameTransform2D(
            ((string?)element.Attribute("name")) ?? $"xform/{index:0000}",
            matrix,
            translation,
            ParseOptionalFloat(element, "weight", 1.0f),
            ParseOptionalFloat(element, "color", 0.0f),
            new FractalFlameVariationWeights(
                ParseOptionalFloat(variationSource, "linear", 0.0f) + ParseOptionalFloat(variationSource, "normal", 0.0f),
                ParseOptionalFloat(variationSource, "spherical", 0.0f),
                ParseOptionalFloat(variationSource, "bubble", 0.0f),
                ParseOptionalFloat(variationSource, "disc", 0.0f) + ParseOptionalFloat(variationSource, "jwf_disc", 0.0f),
                ParseOptionalFloat(variationSource, "julian", 0.0f) + ParseOptionalFloat(variationSource, "jwf_julian", 0.0f),
                ParseOptionalFloat(variationSource, "julian_power", ParseOptionalFloat(variationSource, "jwf_julian_power", 2.0f)),
                ParseOptionalFloat(variationSource, "julian_dist", ParseOptionalFloat(variationSource, "jwf_julian_dist", 1.0f)),
                ParseOptionalFloat(variationSource, "gaussian_blur", 0.0f) + ParseOptionalFloat(variationSource, "jwf_gaussian_blur", 0.0f)));
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
