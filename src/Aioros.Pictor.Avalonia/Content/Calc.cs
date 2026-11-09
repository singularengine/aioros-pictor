using System.Globalization;
using Avalonia.Data.Converters;

namespace Aioros.Pictor;

/// <summary>
/// A converter for a chart's templates that computes a number from the members of the template's context, written as a
/// sum of members times numbers (<c>OuterX + 30*Cos + 22*Side</c>, <c>Index + 1</c>, <c>Y - 4</c>), compares two such
/// sums (<c>Value &gt; 2500</c>, <c>X + Width + 6 &gt; ContainerWidth</c>) for a visibility, or makes a point of two
/// (<c>OuterX + 10*Cos, OuterY + 10*Sin</c>) for a path's points. Bound to the context
/// itself: <c>X="{Binding Converter={p:Calc 'Cx + 10*Cos'}}"</c>. It keeps to sums so that the web compiles the template
/// to SVG with holes; a product of two members is refused when the XAML is loaded.
/// </summary>
public sealed class Calc : IValueConverter
{
    private readonly CalcExpression _expression;

    /// <summary>A converter computing an expression over the context's members.</summary>
    /// <param name="expression">The expression.</param>
    public Calc(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        Expression = expression;
        _expression = CalcExpression.Parse(expression);
    }

    /// <summary>The expression.</summary>
    public string Expression { get; }

    /// <summary>The converter itself, as a markup extension gives it.</summary>
    /// <param name="services">The XAML services (unused).</param>
    /// <returns>The converter.</returns>
    public object ProvideValue(IServiceProvider services) => this;

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is IContentContext context
        ? _expression.Evaluate(context)
        : Avalonia.AvaloniaProperty.UnsetValue;

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

/// <summary>An expression of <see cref="Calc"/>: one sum, two compared, or two separated by a comma (a point).</summary>
internal sealed class CalcExpression
{
    private readonly CalcSum _left;
    private readonly CalcSum? _right;
    private readonly string? _comparison;

    private CalcExpression(CalcSum left, string? comparison, CalcSum? right)
    {
        (_left, _comparison, _right) = (left, comparison, right);
    }

    public static CalcExpression Parse(string text)
    {
        var reader = new CalcReader(text);
        var left = reader.Sum();
        var comparison = reader.Comparison();
        var right = comparison is null ? null : reader.Sum();
        reader.End();
        return new CalcExpression(left, comparison, right);
    }

    public object Evaluate(IContentContext context)
    {
        var left = _left.Evaluate(context);
        if (_comparison is null || _right is null)
        {
            return left;
        }

        if (_comparison == ",")
        {
            return new Avalonia.Point(left, _right.Evaluate(context));
        }

        var right = _right.Evaluate(context);
        return _comparison switch
        {
            ">" => left > right,
            ">=" => left >= right,
            "<" => left < right,
            "<=" => left <= right,
            "==" => left == right,
            _ => left != right,
        };
    }
}

/// <summary>A sum of terms, each a number times a member (divided by a number, as the expression divides it) or a number alone.</summary>
internal sealed class CalcSum(double constant, IReadOnlyList<(double Coefficient, double Divisor, string Member)> terms)
{
    public double Constant { get; } = constant;

    public IReadOnlyList<(double Coefficient, double Divisor, string Member)> Terms { get; } = terms;

    public double Evaluate(IContentContext context)
    {
        var value = Constant;
        foreach (var (coefficient, divisor, member) in Terms)
        {
            value += coefficient * context.Number(member) / divisor;
        }

        return value;
    }

    public CalcSum Scaled(double factor) => new(Constant * factor, [.. Terms.Select(term => (term.Coefficient * factor, term.Divisor, term.Member))]);

    public CalcSum Divided(double divisor) => new(Constant / divisor, [.. Terms.Select(term => (term.Coefficient, term.Divisor * divisor, term.Member))]);

    public CalcSum Plus(CalcSum other) => new(Constant + other.Constant, [.. Terms, .. other.Terms]);

    public bool IsConstant => Terms.Count == 0;
}

/// <summary>Reads an expression of <see cref="Calc"/>.</summary>
internal ref struct CalcReader(string text)
{
    private int _position;

    public CalcSum Sum()
    {
        var sum = Product();
        while (Skip() && _position < text.Length && text[_position] is '+' or '-')
        {
            var sign = text[_position++] == '-' ? -1 : 1;
            sum = sum.Plus(Product().Scaled(sign));
        }

        return sum;
    }

    public string? Comparison()
    {
        Skip();
        var at = _position;
        var source = text;
        var found = ((string[])[">=", "<=", "==", "!=", ">", "<", ","]).FirstOrDefault(operation => string.CompareOrdinal(source, at, operation, 0, operation.Length) == 0);
        _position += found?.Length ?? 0;
        return found;
    }

    public void End()
    {
        Skip();
        if (_position < text.Length)
        {
            throw Error($"unexpected '{text[_position]}'");
        }
    }

    private CalcSum Product()
    {
        var product = Factor();
        while (Skip() && _position < text.Length && text[_position] is '*' or '/')
        {
            var divide = text[_position++] == '/';
            var factor = Factor();
            if (divide)
            {
                product = factor.IsConstant && factor.Constant != 0 ? product.Divided(factor.Constant) : throw Error("a division by a member or by zero");
            }
            else if (product.IsConstant)
            {
                product = factor.Scaled(product.Constant);
            }
            else
            {
                product = factor.IsConstant ? product.Scaled(factor.Constant) : throw Error("a product of two members, which the web cannot compile");
            }
        }

        return product;
    }

    private CalcSum Factor()
    {
        Skip();
        if (_position >= text.Length)
        {
            throw Error("a missing term");
        }

        var c = text[_position];
        if (c == '-')
        {
            _position++;
            return Factor().Scaled(-1);
        }

        if (c == '(')
        {
            return Parenthesized();
        }

        return char.IsAsciiDigit(c) || c == '.' ? Literal() : Member(c);
    }

    private CalcSum Parenthesized()
    {
        _position++;
        var inner = Sum();
        Skip();
        if (_position >= text.Length || text[_position] != ')')
        {
            throw Error("a missing ')'");
        }

        _position++;
        return inner;
    }

    /// <summary>A number: ASCII digits with a point, and an exponent whose sign may follow its <c>e</c> (<c>1e-3</c>).</summary>
    private CalcSum Literal()
    {
        var start = _position;
        Digits(allowPoint: true);
        if (_position < text.Length && text[_position] is 'e' or 'E')
        {
            var exponent = _position + 1 < text.Length && text[_position + 1] is '+' or '-' ? _position + 2 : _position + 1;
            if (exponent < text.Length && char.IsAsciiDigit(text[exponent]))
            {
                _position = exponent;
                Digits(allowPoint: false);
            }
        }

        return double.TryParse(text.AsSpan(start, _position - start), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? new CalcSum(number, [])
            : throw Error($"the number '{text[start.._position]}'");
    }

    private void Digits(bool allowPoint)
    {
        while (_position < text.Length && (char.IsAsciiDigit(text[_position]) || (allowPoint && text[_position] == '.')))
        {
            _position++;
        }
    }

    private CalcSum Member(char first)
    {
        var start = _position;
        while (_position < text.Length && (char.IsLetterOrDigit(text[_position]) || text[_position] is '_' or '.' or '[' or ']'))
        {
            _position++;
        }

        return _position > start ? new CalcSum(0, [(1, 1, text[start.._position])]) : throw Error($"unexpected '{first}'");
    }

    private bool Skip()
    {
        while (_position < text.Length && char.IsWhiteSpace(text[_position]))
        {
            _position++;
        }

        return true;
    }

    private readonly FormatException Error(string what) => new($"Calc '{text}': {what} at {_position}.");
}
