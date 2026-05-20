using System.Globalization;
using System.Numerics;
using System.Xml.Linq;

namespace Aquarium.Engine.Fractal.Grammar;

public static class FractalFlameFileParser
{
    public static FractalFlameDefinition ParseFirst(string source, int seed = 0)
    {
        return ParseFirstWithReport(source, seed).Definition;
    }

    public static FractalFlameParseResult ParseFirstWithReport(string source, int seed = 0)
    {
        ArgumentNullException.ThrowIfNull(source);

        var document = XDocument.Parse(source);
        var flame = document.Descendants("flame").FirstOrDefault()
            ?? throw new FormatException("Flame file does not contain a flame element.");
        var name = ((string?)flame.Attribute("name")) ?? "flame";
        var isJWildfireDialect = string.Equals(document.Root?.Name.LocalName, "Flames", StringComparison.Ordinal)
            || flame.Elements("xform").Any(xform => xform.Element("variationGroup") is not null);
        var parsed = flame.Elements("xform").Select((xform, index) => ParseXform(xform, index, isJWildfireDialect)).ToArray();
        var definition = new FractalFlameDefinition(name, Vector2.Zero, seed, parsed.Select(row => row.Transform).ToArray());
        var report = new FractalFlameImportReport(
            name,
            isJWildfireDialect ? "JWildfire" : "Apophysis/FLAM3 subset",
            parsed.Length,
            BuildFlameFieldReports(flame),
            parsed.Select(row => row.Report).ToArray());
        return new FractalFlameParseResult(definition, report);
    }

    private static ParsedXform ParseXform(XElement element, int index, bool isJWildfireDialect)
    {
        var coefs = ParseFloatList((string?)element.Attribute("coefs"), expectedCount: 6, "xform coefs");
        var variationGroup = element.Element("variationGroup");
        var variationSource = variationGroup ?? element;
        var accepted = new List<string> { "coefs", "weight", "color" };
        var approximated = new List<string>();
        var ignored = new List<string>();
        var rejected = new List<string>();
        if (element.Attribute("name") is not null)
        {
            accepted.Add("name");
        }

        if (isJWildfireDialect)
        {
            accepted.Add("coefs: JWildfire xy layout");
        }

        if (variationGroup is not null)
        {
            accepted.Add("variationGroup");
        }

        var matrix = isJWildfireDialect
            ? new Vector4(coefs[0], coefs[2], coefs[1], coefs[3])
            : new Vector4(coefs[0], coefs[1], coefs[3], coefs[4]);
        var translation = isJWildfireDialect
            ? new Vector2(coefs[4], coefs[5])
            : new Vector2(coefs[2], coefs[5]);
        var variations = new FractalFlameVariationWeights(
            ParseOptionalFloat(variationSource, "linear", 0.0f) + ParseOptionalFloat(variationSource, "normal", 0.0f),
            ParseOptionalFloat(variationSource, "spherical", 0.0f),
            ParseOptionalFloat(variationSource, "bubble", 0.0f),
            ParseOptionalFloat(variationSource, "disc", 0.0f) + ParseOptionalFloat(variationSource, "jwf_disc", 0.0f),
            ParseOptionalFloat(variationSource, "julian", 0.0f) + ParseOptionalFloat(variationSource, "jwf_julian", 0.0f),
            ParseOptionalFloat(variationSource, "julian_power", ParseOptionalFloat(variationSource, "jwf_julian_power", 2.0f)),
            ParseOptionalFloat(variationSource, "julian_dist", ParseOptionalFloat(variationSource, "jwf_julian_dist", 1.0f)),
            ParseOptionalFloat(variationSource, "gaussian_blur", 0.0f) + ParseOptionalFloat(variationSource, "jwf_gaussian_blur", 0.0f));
        ClassifyVariationFields(variationSource, accepted, approximated, ignored, rejected);
        ClassifyXformFields(element, variationGroup, accepted, ignored, rejected);
        var transformName = ((string?)element.Attribute("name")) ?? $"xform/{index:0000}";

        var transform = new FractalFlameTransform2D(
            transformName,
            matrix,
            translation,
            ParseOptionalFloat(element, "weight", 1.0f),
            ParseOptionalFloat(element, "color", 0.0f),
            variations);
        return new ParsedXform(transform, new FractalFlameTransformImportReport(
            index,
            transformName,
            accepted.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            approximated.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            ignored.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray(),
            rejected.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToArray()));
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

    private static IReadOnlyList<string> BuildFlameFieldReports(XElement flame)
    {
        var structural = new HashSet<string>(StringComparer.Ordinal)
        {
            "name",
            "size",
            "center",
            "scale",
            "rotate",
        };
        return flame.Attributes()
            .Select(attribute => attribute.Name.LocalName)
            .Where(name => !structural.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static void ClassifyVariationFields(
        XElement variationSource,
        List<string> accepted,
        List<string> approximated,
        List<string> ignored,
        List<string> rejected)
    {
        var supported = new HashSet<string>(StringComparer.Ordinal)
        {
            "linear",
            "spherical",
            "bubble",
            "disc",
            "jwf_disc",
            "julian",
            "jwf_julian",
            "julian_power",
            "jwf_julian_power",
            "julian_dist",
            "jwf_julian_dist",
        };
        foreach (var attribute in variationSource.Attributes())
        {
            var name = attribute.Name.LocalName;
            if (supported.Contains(name))
            {
                accepted.Add($"variation:{name}");
                continue;
            }

            if (name is "normal")
            {
                approximated.Add("variation:normal -> linear");
                continue;
            }

            if (name is "gaussian_blur" or "jwf_gaussian_blur")
            {
                approximated.Add($"variation:{name} -> stochastic gaussian_blur");
                continue;
            }

            if (TryParseFloat(attribute.Value, out var value) && value == 0.0f)
            {
                ignored.Add($"variation:{name}=0");
                continue;
            }

            rejected.Add($"variation:{name}");
        }
    }

    private static void ClassifyXformFields(
        XElement element,
        XElement? variationGroup,
        List<string> accepted,
        List<string> ignored,
        List<string> rejected)
    {
        var handled = new HashSet<string>(StringComparer.Ordinal)
        {
            "name",
            "coefs",
            "weight",
            "color",
        };
        foreach (var attribute in element.Attributes())
        {
            var name = attribute.Name.LocalName;
            if (handled.Contains(name))
            {
                continue;
            }

            if (name is "post")
            {
                if (IsIdentityPost(attribute.Value))
                {
                    ignored.Add("post=identity");
                }
                else
                {
                    rejected.Add("post");
                }

                continue;
            }

            if (name is "chaos")
            {
                if (IsNeutralFloatList(attribute.Value, 1.0f))
                {
                    ignored.Add("chaos=all-ones");
                }
                else
                {
                    rejected.Add("chaos");
                }

                continue;
            }

            if (name.StartsWith("wfield_", StringComparison.Ordinal))
            {
                rejected.Add(name);
                continue;
            }

            ignored.Add(name);
        }

        if (variationGroup is not null)
        {
            foreach (var child in element.Elements())
            {
                if (child != variationGroup)
                {
                    ignored.Add(child.Name.LocalName);
                }
            }
        }
    }

    private static bool IsIdentityPost(string value)
    {
        var values = ParseFloatList(value, 6, "post");
        return values[0] == 1.0f
            && values[1] == 0.0f
            && values[2] == 0.0f
            && values[3] == 1.0f
            && values[4] == 0.0f
            && values[5] == 0.0f;
    }

    private static bool IsNeutralFloatList(string value, float neutral)
    {
        var values = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return values.Length > 0 && values.All(item => TryParseFloat(item, out var parsed) && parsed == neutral);
    }

    private static bool TryParseFloat(string source, out float value)
    {
        return float.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private readonly record struct ParsedXform(FractalFlameTransform2D Transform, FractalFlameTransformImportReport Report);
}

public sealed record FractalFlameParseResult(
    FractalFlameDefinition Definition,
    FractalFlameImportReport Report);

public sealed record FractalFlameImportReport(
    string FlameName,
    string Dialect,
    int TransformCount,
    IReadOnlyList<string> IgnoredFlameAttributes,
    IReadOnlyList<FractalFlameTransformImportReport> Transforms)
{
    public int AcceptedFieldCount => Transforms.Sum(transform => transform.AcceptedFields.Count);

    public int ApproximatedFieldCount => Transforms.Sum(transform => transform.ApproximatedFields.Count);

    public int IgnoredFieldCount => IgnoredFlameAttributes.Count + Transforms.Sum(transform => transform.IgnoredFields.Count);

    public int RejectedFieldCount => Transforms.Sum(transform => transform.RejectedFields.Count);
}

public sealed record FractalFlameTransformImportReport(
    int Index,
    string Name,
    IReadOnlyList<string> AcceptedFields,
    IReadOnlyList<string> ApproximatedFields,
    IReadOnlyList<string> IgnoredFields,
    IReadOnlyList<string> RejectedFields);
