# Coverage

[docs/coverage.md](../coverage.md) is the definition of done for the port; these criteria keep it backed by tests.
Its status tables are the tables whose header has a `Status` column and ends with a `Criteria` column; its fidelity
matrix is the table under "Fidelity matrix". A criteria cell lists identifiers (`CHARTS-01`) and ranges
(`POLAR-01..05`, every identifier between both ends). The marker line `Finished: false` or `Finished: true` near the
top says whether the port is declared finished.

| Id | Criterion |
|---|---|
| COVERAGE-01 | Given docs/coverage.md, when its status tables are read, then every row's status is `done`, `partial`, `missing`, `n/a` (each possibly followed by a note) or `—` for a Pictor extra, and every `done` row names at least one criterion; a `done` row with an empty Criteria cell, or a row of another status, fails the test naming the row. |
| COVERAGE-02 | Given docs/coverage.md, when the identifiers in the Criteria cells of its status tables and in the cells of its fidelity matrix are read (ranges expanded), then each is a criterion defined in docs/acceptance; an identifier that no acceptance file defines fails the test naming it and its row. |
| COVERAGE-03 | Given the fidelity matrix, when its cells are read, then each cell holds criteria, `n/a` followed by a reason, or nothing; a cell holding anything else, or `n/a` without a reason, fails the test; the empty cells are listed in the test's output, and they fail the test only when the marker reads `Finished: true`. |
| COVERAGE-04 | Given docs/coverage.md, when its status tables are read, then the rows that are neither `done` nor `n/a` are listed in the test's output, and they fail the test only when the marker reads `Finished: true`; a file without the marker fails the test. |
| COVERAGE-05 | Given the counts line at the top of docs/coverage.md (`Counts: done D · partial P · missing M · n/a N · fidelity matrix F of T cells filled`), when the status tables and the matrix are counted (Pictor extras left out, a cell filled when it holds criteria or `n/a`), then each number equals the count; a number that differs, or a missing counts line, fails the test with both values. |
