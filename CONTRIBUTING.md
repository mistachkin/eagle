# Contributing to Eagle

Thank you for your interest in contributing to Eagle.  This document
describes the coding conventions, rules, and procedures that all
contributions must follow.  Pull requests that do not conform to these
rules will be rejected.

## Legal

All contributions must be dedicated to the public domain **or** signed
over to the project administrator via a signed copyright release.  By
submitting a pull request you acknowledge this requirement.

### Contributor License Agreement (CLA)

This repository uses the [CLA Assistant](https://github.com/contributor-assistant/github-action)
to automate contributor license management.  When you open a pull request:

1. The CLA Assistant bot will post a comment with a link to the
   [CLA](CLA.md).
2. Read the CLA and choose **Option A** (public domain dedication) or
   **Option B** (copyright assignment).
3. Post the following comment on the pull request to sign:
   **"I have read the CLA Document and I hereby sign the CLA"**
4. The `cla-check` status will update automatically once your signature
   is recorded.

Your signature is stored in the repository and applies to all future
contributions -- you only need to sign once.

## General Rules

### Line Length

All C# source code and Eagle script lines must be "soft" broken at the
**79-character** mark, or at the limit used by the surrounding code.

Certain legacy files have longer line lengths; always make sure to match
the convention already in use in the file you are modifying.

### No LINQ

Use of `System.Core`, `System.Linq`, LINQ query syntax, and related
extension methods is **forbidden** throughout the codebase.

### No New Dependencies

Adding NuGet packages, assembly references, or other external
dependencies is **forbidden** unless explicitly approved by the project
administrator.  If an approved dependency is added, there must be
fallback behavior at both compile-time (via `#if`) and runtime.  All
applicable MSBuild project files must be kept updated with the
necessary conditionals.

### Script-Level Backward Compatibility

In general, backward compatibility for script constructs must be
maintained at all times.  Additionally, compatibility with the Tcl
8.4 language specification is required.  Exceptions may be made;
however, they will require approval by the project administrator.

### .NET Framework 2.0 RTM Compatibility

All C# source code must compile and build successfully against the
**.NET Framework 2.0 RTM** (or later) using its shipped C# compiler.
Any use of newer language features or APIs must be wrapped in
preprocessor directives (e.g., `#if NET_40`, `#if NET_STANDARD_21`).
These features must be optional and the surrounding behavior must
degrade gracefully when they are not present.

### Exception Handling

All new code that is capable of throwing exceptions must handle them in
such a way that they will not cause an unhandled exception for the
process.  For native interop via P/Invoke (which must only be used for
optional features), this generally means catching all exceptions and
logging them via the existing tracing infrastructure.

### Tests Required

- All **new functionality** must have accompanying tests.
- All **bug fixes** must have a regression test.

### Change Log

All major new or changed functionality must be documented in the change
log file (`ChangeLog`) following its existing conventions.

### Coding Agents

Coding agents (AI assistants, copilots, etc.) may be used; however, the
resulting code must still follow every rule in this document and must be
easy for a human to understand and review.

### Comments

Any algorithms and/or code that are not immediately obvious to a junior
engineer must have explanatory block comments formatted according to the
existing conventions:

```csharp
//
// NOTE: Explanation of what this code does and why.
//
```

Other recognized comment prefixes include `HACK:`, `TODO:`, `WARNING:`,
`BUGBUG:`, and `FIXME:`.

### Safe Interpreter Security

All new commands, sub-commands, and options must be evaluated for their
impact on **safe** interpreters:

- If the parent command is already marked **unsafe**, adding sub-commands
  is relatively straightforward, but the implications must still be
  documented.

- If the parent command is **safe**, any new sub-commands must not
  violate the safety guarantees.  This includes disclosure of
  information that is not already available within a **safe** interpreter
  context.

- Any new options to commands or sub-commands that are allowed in a **safe**
  interpreter must be marked as **unsafe** (via `CommandFlags`) until
  they have been vetted by the project administrator.

### ObjectId Attribute

All new classes and structs must have an `[ObjectId("...")]` attribute
with a freshly generated GUID value.  Other attributes may be required
as well, depending on the subsystem of the file being added.

### Thread Safety

All new public and internal types must document their thread-safety
guarantees.  Shared mutable state must be protected with appropriate
synchronization.  Follow the existing patterns in the codebase (e.g.,
`lock` on a dedicated `syncRoot` object).  If there is a possibility
of deadlock, the **TryLock pattern** must be used.

### XML Documentation Comments

Every C# type and member -- **public, internal, and private** alike
(classes, structs, interfaces, enums, delegates, fields, constants,
properties, methods, operators, constructors, and events) -- must carry
an XML documentation comment.  This applies to all new code without
exception; a contribution that adds an undocumented type or member will
be rejected.

- The documentation comment is one contiguous run of `///` lines placed **above**
  the member's attributes (and above any `#if`-guarded attribute
  block); a preprocessor directive must never appear inside a documentation
  comment.
- Each tag is on its own line.  Every parameter has a `<param>`
  (including parameters that are conditionally compiled out); every
  non-void member has a `<returns>`; every generic type or method has a
  `<typeparam>`.
- Reference types and members with `<see cref="..." />` and parameters
  with `<paramref name="..." />`.  Never use `<see langword=...>`, and
  never `cref` a parameter.
- Members inside `#if DEAD_CODE` (or other compiled-out) blocks are
  documented too -- such code is intentionally preserved and may be
  revived, so its contract must remain described.

When you **modify** an existing member, you must update its XML
documentation comment so it continues to match the code whenever your
change affects the behavior, parameters, return value, exceptions, or
any other part of the contract the comment describes.  A documentation comment
that no longer matches the code it describes is treated as a defect.

For the full conventions, placement rules, and worked examples, see
**"Comprehensive XML Documentation" (convention 25)** -- together with
the related conventions 18 through 27 -- in `architecture_patterns.md`
in the [documentation repository](https://urn.to/r/docs) (this will
redirect).

### Documentation Repository

The [Eagle documentation repository](https://urn.to/r/docs) (this will redirect)
must be kept up-to-date with changes in this repository.  If your pull
request adds, changes, or removes any user-facing functionality (commands,
sub-commands, options, configuration, APIs, etc.), a corresponding pull
request to the documentation repository is required and must be linked
in your PR description.

### No Unused Code

Do not leave dead code, commented-out blocks, or any unused `using`
statements in your contribution.  Remove anything that is not actively
used.

---

## C# Conventions

### File Header

Every C# file must begin with the standard copyright header:

```csharp
/*
 * FileName.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */
```

### Indentation and Formatting

- **4 spaces** per indentation level.  No tabs.
- **Allman style** braces for namespace and class declarations (opening
  brace on its own line).
- **Same-line braces** for method bodies, control structures, and
  property accessors.
- One blank line between methods.
- Trailing spaces are forbidden.
- Section separators use a line of slashes:
  ```csharp
  ///////////////////////////////////////////////////////////////////////
  ```

### Naming

| Element            | Convention   | Example                          |
|--------------------|--------------|----------------------------------|
| Namespace          | PascalCase   | `Eagle._Commands`                |
| Class / Struct     | PascalCase   | `ObjectIdAttribute`              |
| Interface          | IPascalCase  | `IExecute`                       |
| Public method      | PascalCase   | `Execute()`                      |
| Public property    | PascalCase   | `SubCommands`                    |
| Private field      | camelCase    | `defaultSubSubCommands`          |
| Local variable     | camelCase    | `localResult`                    |
| Parameter          | camelCase    | `interpreter`                    |
| Constant           | PascalCase   | `DefaultNoCase`                  |
| Enum member        | PascalCase   | `CommandFlags.Safe`              |

### Using Statements

- System namespaces first, then Eagle namespaces, then other namespaces,
  and finally type alises, etc.
- Alphabetical order within each group.
- Conditional `using` directives wrapped in `#if` when needed:
  ```csharp
  #if NET_STANDARD_21
  using Index = Eagle._Constants.Index;
  #endif
  ```

### Regions

Use `#region` / `#endregion` blocks to organize code within classes.
Follow the existing ordering:

1. Private Constants
2. Private Static Data
3. Private Data
4. Public Data
5. Constructors
6. Interface implementation members (grouped by interface)
7. IDisposable pattern members

### Parameter Annotations

Use inline comments to indicate parameter direction when the intent is
not obvious:

```csharp
private void Initialize(
    ref EnsembleDictionary subCommands /* in, out */
    )
```

Other recognized inline annotations include `/* throw */`, `/* EXEMPT */`,
and `/* TRANSACTIONAL */`.

### Preprocessor Directives

- Use existing symbols: `DEBUG`, `PATCHLEVEL`, `NET_STANDARD_21`,
  `NET_40`, `THROW_ON_DISPOSED`, `SHELL`, etc.
- New symbols must be documented and added to all relevant MSBuild
  project files.
- All `#if`-guarded code must compile cleanly with and without the
  symbol defined.

---

## Eagle Script Conventions

### File Header

Every Eagle script file must begin with the standard copyright header:

```tcl
###############################################################################
#
# filename.eagle --
#
# Extensible Adaptable Generalized Logic Engine (Eagle)
#
# Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
#
# See the file "license.terms" for information on usage and redistribution of
# this file, and for a DISCLAIMER OF ALL WARRANTIES.
#
# RCS: @(#) $Id: $
#
###############################################################################
```

### Indentation and Formatting

- **2 spaces** per indentation level.  No tabs.
- Same-line brace style (opening brace on the same line as the
  statement):
  ```tcl
  if {$condition} then {
    # body
  } else {
    # alternative
  }
  ```
- Use `then` with `if` statements for clarity.
- Closing brace on its own line at the same indentation level as the
  opening statement.
- `} else {` and `} elseif {` on the same line as the closing brace.

### Naming

| Element            | Convention   | Example                          |
|--------------------|--------------|----------------------------------|
| Procedure          | camelCase    | `getTclReserved`                 |
| Local variable     | camelCase    | `savedFlags`                     |
| Global variable    | snake\_case  | `tcl_platform`                   |
| Environment var    | UPPER\_CASE  | `EAGLE_TEST_TEMP`                |

### Comments

- Use `#` for all comments.
- Block comments use the pattern:
  ```tcl
  #
  # NOTE: Explanation of what this code does.
  #
  ```
- Recognized prefixes: `NOTE:`, `HACK:`, `TODO:`, `WARNING:`, `MONO:`,
  `BUGBUG:`.
- Comments are indented to match the surrounding code block.

### String Quoting

- Prefer **curly braces** `{}` for literal strings and bodies (no
  substitution, better performance).
- Use **double quotes** `""` only when variable or command substitution
  is required.

### Section Separators

Use a line of hash marks between procedures and between test cases:

```tcl
###############################################################################
```

### Test Files

- Test files begin with `source [file join ... prologue.eagle]` to load
  the test framework.
- Tests use the `runTest` wrapper:
  ```tcl
  runTest {test testname-1.1 {description} -setup {
    # setup
  } -body {
    # test body
  } -cleanup {
    # cleanup
  } -constraints {constraint} -result {expected}}
  ```
- Test names follow the pattern `category-major.minor` with sequential
  numbering.
- Use `-constraints` to gate tests on platform, feature, or
  configuration.
- Use `unset -nocomplain` in cleanup blocks.

### Script Signing

Any pull request that modifies Eagle scripts in the repository must be
tagged with **"scripts"** so the project administrator knows those files
need to be re-signed for use with the Harpy / Badge plugins.

If any core script library (`Eagle1.0`) or test package library
(`Test1.0`) scripts are modified, the associated embedded resource file
must be rebuilt:

- `Eagle1.0` - `Library/Resources/library.resources`
- `Test1.0` - `Library/Resources/packages.resources`

---

## Pull Request Checklist

Before submitting a pull request, verify that:

- [ ] Code compiles against .NET Framework 2.0 RTM (no newer APIs
      without `#if` guards).
- [ ] Code compiles against .NET Standard 2.1 / .NET 10.0.
- [ ] No LINQ usage anywhere in the change.
- [ ] No new dependencies added without prior approval.
- [ ] All new classes and structs have `[ObjectId("...")]` with a new
      GUID.
- [ ] All new types and members have conforming XML documentation
      comments, and any modified members' doc comments were updated
      to reflect behavioral changes.
- [ ] All exceptions are caught and handled appropriately.
- [ ] All **safe** interpreter implications have been considered for
      any new commands, sub-commands, or options.
- [ ] Line length does not exceed the file's convention (79 or
      100/104 columns).
- [ ] New functionality has tests.
- [ ] Bug fixes have regression tests.
- [ ] The `ChangeLog` has been updated.
- [ ] If Eagle scripts were modified, the PR is tagged **"scripts"**.
- [ ] If `Eagle1.0` or `Test1.0` library scripts were modified, the
      embedded resource files have been rebuilt.
- [ ] All CI checks pass on both Linux and macOS.
- [ ] If user-facing functionality was added or changed, a corresponding
      PR to the
      [documentation repository](https://urn.to/r/docs) (this will redirect)
      has been opened and linked.
