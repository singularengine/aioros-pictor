# Conventions

Design rules that apply across the library, for anyone changing it.

## Sealed classes

Every class is `sealed` or `static`, with a few exceptions that the architecture tests read from the line below.

- **Abstract base classes kept:** `Chart`, `CartesianChart`, `PolarChart`, `Axis`, `Series`, `PointSeries`, `PolarElement`, `PolarSeries`. They are Avalonia elements: XAML, styles (`p|Series`), control themes and the property system (a `StyledProperty` registered once on its owner and inherited by every series that has it) work through the class hierarchy, which Avalonia imposes as BenchmarkDotNet imposes open benchmark classes; composition would duplicate every property per element and break the styles apps write. No other class is abstract, and the architecture test rejects an abstract class this line does not name.
