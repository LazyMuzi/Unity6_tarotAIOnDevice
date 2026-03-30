---
description: 
alwaysApply: true
---

# AGENTS.md

## Purpose
This document serves as an operational guide to help AI coding agents maintain consistent quality standards in this Unity project.

## Communication
- All user responses must be written in Korean.
- Briefly explain the intent, scope of impact, and verification method for all changes.
- Only ask questions when requirements are unclear, and propose actionable alternatives first.

## Core Engineering Principles
- Adhere to SOLID principles.
- Maintain a structure with clear separation of responsibilities.
- Avoid concentrating excessive responsibility in a single class.
- Prioritize clean architecture and scalability.
- Only propose/produce production-ready code.

## Unity/C# Rules
- Avoid using `FindObjectOfType`.
- Design with DI (Dependency Injection) in mind.
- Avoid hardcoded strings; use constants or configurations.
- Add XML summary comments to public classes.
- Use async/await correctly in asynchronous code.
- Separate testable pure logic from UI/engine-dependent code.

## Testing Strategy (TDD)
- Prioritize TDD for core domain logic.
- For high-overhead areas like UI binding or scene wiring, supplement with integration or manual testing.
- Every new feature must include at least one regression-prevention testing strategy.

## Change Safety Rules
- Do not revert existing changes that the user did not request.
- Avoid destructive/irreversible git commands (e.g., `git reset --hard`, forced checkout).
- Only commit/push when explicitly requested by the user.

## Working Process
1. First, review the relevant files and context.
2. If the work is significant in scope, share a brief plan beforehand.
3. Make minimal changes while maintaining quality standards.
4. After modification, check for test/lint/build validity and viability.
5. Report changed files, reasons, verification results, and follow-up suggestions.

## Code Quality Checklist
- [ ] Is the separation of responsibilities clear?
- [ ] Is the structure extensible?
- [ ] Are hardcoded strings eliminated?
- [ ] Is there no misuse of Unity's object-finding APIs?
- [ ] Are asynchronous exceptions/cancellations handled safely?
- [ ] Is there a method for testing or verification?
