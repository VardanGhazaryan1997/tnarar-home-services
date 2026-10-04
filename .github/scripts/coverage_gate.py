"""Fails the build when backend coverage is below MIN_LINE_COVERAGE / MIN_BRANCH_COVERAGE,
or when fewer than EXPECTED_ASSEMBLIES projects were measured (coverlet skips an assembly it
cannot instrument with only a warning, which would make the numbers look better than they are).

Reads ReportGenerator's Summary.json (JsonSummary report type).
Usage: python3 coverage_gate.py path/to/Summary.json
"""
import json
import os
import sys


def main(path: str) -> int:
    with open(path, encoding="utf-8") as file:
        summary = json.load(file)["summary"]

    checks = [
        ("Line", summary.get("linecoverage") or 0.0, float(os.environ.get("MIN_LINE_COVERAGE", "90"))),
        ("Branch", summary.get("branchcoverage") or 0.0, float(os.environ.get("MIN_BRANCH_COVERAGE", "85"))),
    ]

    failed = False

    expected = int(os.environ.get("EXPECTED_ASSEMBLIES", "0"))
    measured = summary.get("assemblies") or 0
    if measured < expected:
        print(f"Only {measured} of {expected} projects were measured; one could not be instrumented.")
        failed = True
    for name, actual, minimum in checks:
        status = "ok" if actual >= minimum else "TOO LOW"
        print(f"{name} coverage: {actual:.1f}% (minimum {minimum:.0f}%) {status}")
        failed |= actual < minimum

    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
