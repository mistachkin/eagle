## Description

<!-- What does this PR do? Why is it needed? -->

## Related Issues

<!-- Link any related issues: Fixes #123, Closes #456 -->

## Type of Change

- [ ] Bug fix
- [ ] New feature
- [ ] Enhancement to existing functionality
- [ ] Refactoring (no functional change)
- [ ] Documentation
- [ ] Test coverage

## Checklist

Please verify the following before requesting review.  See
[CONTRIBUTING.md](../CONTRIBUTING.md) for full details.

### Compilation

- [ ] Code compiles against .NET Framework 2.0 RTM (no newer APIs
      without `#if` guards).
- [ ] Code compiles against .NET Standard 2.1 / .NET 10.0.

### Coding Rules

- [ ] No `System.Linq` or LINQ query syntax anywhere in the change.
- [ ] No new dependencies added without prior approval.
- [ ] All new classes and structs have `[ObjectId("...")]` with a new
      GUID.
- [ ] All exceptions are caught and handled appropriately.
- [ ] Line length does not exceed the file's convention (79 or
      100/104 columns).
- [ ] Non-obvious code has explanatory block comments (`// NOTE: ...`).

### Safety

- [ ] Safe interpreter implications have been considered for any new
      commands, sub-commands, or options.

### Tests and Documentation

- [ ] New functionality has tests.
- [ ] Bug fixes have regression tests.
- [ ] The `ChangeLog` has been updated.
- [ ] All CI checks pass on both Linux and macOS.
- [ ] If user-facing functionality was added or changed, a corresponding
      PR to the
      [documentation repository](https://github.com/mistachkin/docs)
      has been opened and linked.

### Script Changes (if applicable)

- [ ] PR is tagged **"scripts"** (Eagle scripts were modified).
- [ ] Embedded resource files rebuilt if `Eagle1.0` or `Test1.0`
      library scripts were modified.

### Legal

- [ ] I have read and signed the [Contributor License Agreement](../CLA.md),
      or I am listed in the CLA allowlist.
