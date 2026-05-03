#!/usr/bin/env python3
"""B.3-16 Batch Replacement Tool

Replaces typeof(long/string/double/void) with EraType enum equivalents
in Creator.Method.cs and ArgumentBuilder.cs (B.3-16b/16c scope).

Also provides checklist generation and diff comparison for verification.

Usage:
  python tools/b3_16_replace.py --dry-run          # Preview all replacements, no file changes
  python tools/b3_16_replace.py --checklist         # Generate replacement checklist (CSV)
  python tools/b3_16_replace.py --apply             # Apply replacements to files
  python tools/b3_16_replace.py --diff              # Show diff of changes (requires --apply first or --dry-run)
  python tools/b3_16_replace.py --verify            # Count remaining typeof after replacement
  python tools/b3_16_replace.py --all-files         # Include all B.3-16 files (not just 16b/16c)
"""

import argparse
import csv
import difflib
import io
import os
import re
import sys
from pathlib import Path
from dataclasses import dataclass, field

EMUERA_DIR = Path(__file__).resolve().parent.parent / "Emuera"

FILES_16B = [
    "Runtime/Script/Statements/Function/Creator.Method.cs",
]

FILES_16C = [
    "Runtime/Script/Statements/ArgumentBuilder.cs",
]

FILES_16D = [
    "Runtime/Script/Statements/Expression/OperatorMethod.cs",
]

FILES_16E = [
    "Runtime/Script/Statements/Variable/VariableToken.cs",
    "Runtime/Script/Statements/Function/UserDefinedMethodTerm.cs",
    "Runtime/Script/Statements/Function/FunctionMethodTerm.cs",
    "Runtime/Script/Statements/Function/UserDefinedRefMethod.cs",
    "Runtime/Script/Statements/Instraction.Child.cs",
    "Runtime/Script/Statements/Expression/AExpression.cs",
    "Runtime/Script/Parser/LogicalLineParser.cs",
    "Runtime/Script/Data/StrForm.cs",
    "Runtime/Script/Statements/Expression/ExpressionParser.cs",
    "Runtime/Script/Statements/LogicalLine.cs",
    "Runtime/Script/Statements/CaseExpression.cs",
    "Runtime/Utils/EvilMask/Utils.cs",
]

FILES_16F = [
    "Runtime/Script/Statements/Function/FunctionMethod.cs",
]

SKIP_FILES = [
    "Runtime/Script/SparseArray.cs",
    "Runtime/Utils/尊尼获加/SqlManager.cs",
]

TYPEOF_MAP = {
    "typeof(long)": "EraType.Integer",
    "typeof(string)": "EraType.String",
    "typeof(double)": "EraType.Float",
    "typeof(void)": "EraType.Void",
}

TYPE_ARRAY_MAP = {
    "new Type[]": "new EraType[]",
    "Type[] types": "EraType[] types",
    "Type[] argumentTypeArray": "EraType[] argumentTypeArray",
}

@dataclass
class Replacement:
    file: str
    line_num: int
    original: str
    replaced: str
    pattern: str
    batch: str

def detect_encoding(filepath: Path) -> str:
    with open(filepath, 'rb') as f:
        raw = f.read(4)
    if raw[:3] == b'\xef\xbb\xbf':
        return 'utf-8-sig'
    return 'utf-8'

def read_file(filepath: Path) -> tuple[list[str], str]:
    enc = detect_encoding(filepath)
    with open(filepath, 'r', encoding=enc) as f:
        lines = f.readlines()
    return lines, enc

def write_file(filepath: Path, lines: list[str], enc: str):
    with open(filepath, 'w', encoding=enc, newline='') as f:
        f.writelines(lines)

def classify_batch(rel_path: str) -> str:
    for f in FILES_16B:
        if rel_path.replace('\\', '/').endswith(f.replace('\\', '/')):
            return "16b"
    for f in FILES_16C:
        if rel_path.replace('\\', '/').endswith(f.replace('\\', '/')):
            return "16c"
    for f in FILES_16D:
        if rel_path.replace('\\', '/').endswith(f.replace('\\', '/')):
            return "16d"
    for f in FILES_16E:
        if rel_path.replace('\\', '/').endswith(f.replace('\\', '/')):
            return "16e"
    for f in FILES_16F:
        if rel_path.replace('\\', '/').endswith(f.replace('\\', '/')):
            return "16f"
    return "??"

def get_target_files(all_files: bool) -> list[Path]:
    rel_paths = list(FILES_16B) + list(FILES_16C)
    if all_files:
        rel_paths += list(FILES_16D) + list(FILES_16E) + list(FILES_16F)
    result = []
    for rp in rel_paths:
        fp = EMUERA_DIR / rp
        if fp.exists():
            result.append(fp)
        else:
            print(f"WARNING: File not found: {fp}", file=sys.stderr)
    return result

def process_file(filepath: Path) -> tuple[list[str], list[Replacement], list[str]]:
    lines, enc = read_file(filepath)
    new_lines = []
    replacements = []
    rel_path = str(filepath.relative_to(EMUERA_DIR))
    batch = classify_batch(rel_path)

    for i, line in enumerate(lines):
        original = line
        replaced = line
        matched_patterns = []

        for pattern, replacement in TYPEOF_MAP.items():
            if pattern in replaced:
                replaced = replaced.replace(pattern, replacement)
                matched_patterns.append(pattern)

        if replaced != original:
            r = Replacement(
                file=rel_path,
                line_num=i + 1,
                original=original.rstrip('\r\n'),
                replaced=replaced.rstrip('\r\n'),
                pattern='; '.join(matched_patterns),
                batch=batch,
            )
            replacements.append(r)

        new_lines.append(replaced)

    return new_lines, replacements, [enc]

def generate_checklist(all_replacements: list[Replacement], output_path: Path):
    with open(output_path, 'w', encoding='utf-8-sig', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(["Batch", "File", "Line", "Pattern", "Original", "Replaced", "Verified"])
        for r in all_replacements:
            writer.writerow([r.batch, r.file, r.line_num, r.pattern, r.original, r.replaced, ""])

def generate_diff(filepath: Path, new_lines: list[str]) -> list[str]:
    old_lines, _ = read_file(filepath)
    diff = list(difflib.unified_diff(
        old_lines, new_lines,
        fromfile=f"a/{filepath.relative_to(EMUERA_DIR)}",
        tofile=f"b/{filepath.relative_to(EMUERA_DIR)}",
        lineterm='',
    ))
    return diff

def count_typeof(filepath: Path) -> dict[str, int]:
    lines, _ = read_file(filepath)
    counts = {"typeof(long)": 0, "typeof(string)": 0, "typeof(double)": 0, "typeof(void)": 0, "total": 0}
    for line in lines:
        stripped = line.strip()
        if stripped.startswith('//') or stripped.startswith('/*'):
            continue
        for pattern in counts:
            if pattern == "total":
                continue
            if pattern in line:
                counts[pattern] += 1
                counts["total"] += 1
    return counts

def main():
    parser = argparse.ArgumentParser(description="B.3-16 typeof → EraType batch replacement tool")
    parser.add_argument('--dry-run', action='store_true', help='Preview replacements without modifying files')
    parser.add_argument('--checklist', action='store_true', help='Generate CSV checklist of all replacements')
    parser.add_argument('--apply', action='store_true', help='Apply replacements to files')
    parser.add_argument('--diff', action='store_true', help='Show unified diff of changes')
    parser.add_argument('--verify', action='store_true', help='Count remaining typeof after replacement')
    parser.add_argument('--all-files', action='store_true', help='Include all B.3-16 files (not just 16b/16c)')
    parser.add_argument('--output', type=str, default=None, help='Output file for checklist/diff')
    args = parser.parse_args()

    if not any([args.dry_run, args.checklist, args.apply, args.diff, args.verify]):
        parser.print_help()
        return

    target_files = get_target_files(args.all_files)
    all_replacements = []
    file_data = {}

    for fp in target_files:
        new_lines, reps, enc_info = process_file(fp)
        all_replacements.extend(reps)
        file_data[fp] = (new_lines, enc_info[0])

    if args.dry_run:
        print(f"=== B.3-16 Dry Run: {len(all_replacements)} replacements across {len(target_files)} files ===\n")
        by_batch = {}
        for r in all_replacements:
            by_batch.setdefault(r.batch, []).append(r)
        for batch in sorted(by_batch.keys()):
            reps = by_batch[batch]
            print(f"--- Batch {batch}: {len(reps)} replacements ---")
            for r in reps:
                print(f"  {r.file}:{r.line_num}  [{r.pattern}]")
                print(f"    - {r.original}")
                print(f"    + {r.replaced}")
            print()

        print("=== Summary ===")
        for batch in sorted(by_batch.keys()):
            print(f"  Batch {batch}: {len(by_batch[batch])} replacements")
        print(f"  Total: {len(all_replacements)} replacements")

    if args.checklist:
        out_path = Path(args.output) if args.output else Path(__file__).resolve().parent / "b3_16_checklist.csv"
        generate_checklist(all_replacements, out_path)
        print(f"Checklist written to {out_path} ({len(all_replacements)} entries)")

    if args.apply:
        print(f"=== Applying {len(all_replacements)} replacements to {len(file_data)} files ===")
        for fp, (new_lines, enc) in file_data.items():
            rel = fp.relative_to(EMUERA_DIR)
            old_lines, _ = read_file(fp)
            if old_lines == new_lines:
                print(f"  {rel}: no changes")
                continue
            write_file(fp, new_lines, enc)
            changed = sum(1 for a, b in zip(old_lines, new_lines) if a != b)
            print(f"  {rel}: {changed} lines changed (encoding: {enc})")
        print("Done. Run --verify to check remaining typeof counts.")

    if args.diff:
        out_lines = []
        for fp in target_files:
            if fp in file_data:
                new_lines, _ = file_data[fp]
                diff = generate_diff(fp, new_lines)
                if diff:
                    out_lines.extend(diff)
                    out_lines.append('')

        if args.output:
            with open(args.output, 'w', encoding='utf-8') as f:
                f.write('\n'.join(out_lines))
            print(f"Diff written to {args.output}")
        else:
            print('\n'.join(out_lines))

    if args.verify:
        print("=== typeof Residual Count ===")
        all_files = get_target_files(True)
        for skip in SKIP_FILES:
            print(f"  [SKIP] {skip} (intentionally excluded)")
        total = {"typeof(long)": 0, "typeof(string)": 0, "typeof(double)": 0, "typeof(void)": 0}
        for fp in all_files:
            counts = count_typeof(fp)
            rel = fp.relative_to(EMUERA_DIR)
            if counts["total"] > 0:
                parts = [f"{k}={v}" for k, v in counts.items() if v > 0 and k != "total"]
                print(f"  {rel}: {', '.join(parts)}")
            for k in total:
                total[k] += counts.get(k, 0)
        print(f"\n  TOTAL: typeof(long)={total['typeof(long)']}, typeof(string)={total['typeof(string)']}, "
              f"typeof(double)={total['typeof(double)']}, typeof(void)={total['typeof(void)']}")

if __name__ == '__main__':
    main()
