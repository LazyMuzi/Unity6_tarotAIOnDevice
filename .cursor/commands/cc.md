---
description: C# Code Cleanup — dedup, simplify, improve quality
---

Analyze the selected file(s) against the checklist below. Only fix items that actually have problems — skip anything that's already fine. After making changes, provide a summary table of what was modified and why.

## 1. Duplicate Code
- Extract repeated or near-identical logic into shared methods
- Consolidate scattered magic numbers/strings into constants or SerializeField
- Merge redundant conditional branches

## 2. Simplification
- Remove unused fields, methods, and using directives
- Flatten nested ifs with early returns (guard clauses)
- Prefer LINQ, pattern matching, and null-conditional operators (?., ??) where clearer
- Remove unnecessary fully-qualified namespaces (e.g. UnityEngine.Screen → Screen)

## 3. Structure & Design
- Check for SOLID violations (SRP, DIP, etc.)
- Suggest responsibility splits for monolithic classes
- Replace FindObjectOfType with DI or direct references
- Replace hardcoded strings with const, enum, or SerializeField

## 4. Robustness
- Add null checks / guard clauses for public APIs
- Verify OnDestroy cleanup (events, listeners, subscriptions)
- Ensure singletons null out Instance in OnDestroy
- Check for static event subscriber leaks
- Verify exception handling in async/await and coroutines

## 5. Readability
- Add XML summary comments to public classes and methods if missing
- Remove comments that merely restate the code
- Ensure consistent naming conventions (PascalCase / camelCase / m_ prefix) within each file

## Constraints
- Do NOT change existing behavior or functionality
- Output production-ready code only
- Provide a before/after summary table of all changes
