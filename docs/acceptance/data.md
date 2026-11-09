# Data

How a chart reads its bound items: one column per data key, read once per render.

| Id | Criterion |
|---|---|
| DATA-01 | Given items whose value is an int, a decimal, a numeric text, null, a non-numeric text, a dictionary entry and a null item, when the numbers of the key are read, then they are 1, 2.5, 3.25 and NaN, NaN, 7 and NaN: numbers and numeric texts as numbers, anything else a gap. |
| DATA-02 | Given category values that are numbers and texts, when the texts of the key are read, then numbers are written as JavaScript's `String(n)` writes them (`1`, `2.5`, `1e+21`, `0.30000000000000004`) and texts as they are. |
| DATA-03 | Given items without the member a data key names, when the key is read, then every value is a gap (NaN, no text) and nothing fails. |
