# AGENTS.md - Guide for AI Agents Writing Eagle Tests

This document provides guidance for AI agents (such as Claude, GPT, etc.) to write idiomatic tests using the Eagle scripting language and its script library.

## Quick Reference

**Canonical Documentation Location**: The complete Eagle documentation is available at:
- **https://urn.to/r/docs** (this will redirect)

**Documentation Files** in the repository:
| File | Description |
|------|-------------|
| `core_language.md` | Core language reference - all built-in commands, syntax, and .NET integration |
| `core_script_library.md` | Script library reference - procedures from Eagle's standard library packages |
| `README.md` | Documentation index and overview |

**How to Access Documentation**:
1. Navigate to https://urn.to/r/docs (this will redirect)
2. Read `core_language.md` for command syntax and language features
3. Read `core_script_library.md` for library procedures and utilities

**For AI Agents**: When you need detailed information about Eagle commands or procedures, fetch and read the appropriate documentation file from the repository above.

---

## Eagle Overview

Eagle is a Tcl-compatible scripting language with deep .NET/CLR integration. It is designed for:
- Testing .NET applications and libraries
- Scripting within .NET host applications
- Cross-platform automation (Windows, Linux, macOS via Mono/.NET Core)

### Key Differences from Tcl

**IMPORTANT**: Eagle is Tcl-compatible but NOT identical to Tcl. The following Tcl features are **NOT available** in Eagle:

| Tcl Feature | Eagle Alternative |
|-------------|-------------------|
| `{*}` expansion operator | Use `[eval]` with `[list]` for argument expansion |
| `dict` command | Use key-value lists with `getDictionaryValue` |
| `namespace ensemble` | Not supported |
| `namespace path` | Not supported |
| `fileevent` | Use polling with `after` or .NET async patterns |
| `try {} on error {} {}` | Use `try {} finally {}` or `catch` |

---

## Test Infrastructure

### Test File Structure

Eagle test files follow a standard structure:

```tcl
###############################################################################
#
# test-name.eagle -- Description of test suite
#
###############################################################################

package require Eagle
package require Eagle.Library
package require Eagle.Test

runTestPrologue

###############################################################################

# Individual tests go here

###############################################################################

runTestEpilogue
unset -nocomplain test_channel
```

### The `test` Command

The primary test command supports both old-style and new-style syntax. **Always use new-style syntax** with named options:

```tcl
test testName {description} -setup {
    # Setup code (optional)
} -body {
    # Test implementation (required)
} -cleanup {
    # Cleanup code (optional)
} -result {expected result}
```

### Complete Test Options

| Option | Description |
|--------|-------------|
| `-setup` | Code to run before the test body |
| `-body` | The actual test code (required) |
| `-cleanup` | Code to run after the test (always runs, even on failure) |
| `-result` | Expected result value |
| `-output` | Expected stdout output |
| `-errorOutput` | Expected stderr output |
| `-returnCodes` | Expected return codes (`ok`, `error`, `return`, `break`, `continue`) |
| `-match` | Result matching mode: `exact` (default), `glob`, `regexp` |
| `-constraints` | List of constraints that must be satisfied |

### Test Naming Convention

Use hierarchical names: `category-subcategory-number.number`

```tcl
test string-length-1.1 {string length of empty string} -body {
    string length ""
} -result {0}

test string-length-1.2 {string length of simple string} -body {
    string length "hello"
} -result {5}
```

---

## Test Constraints

Constraints allow tests to be skipped when conditions aren't met.

### Common Built-in Constraints

| Constraint | Description |
|------------|-------------|
| `eagle` | Running under Eagle (always true in Eagle) |
| `tcl` | Running under Tcl (always false in Eagle) |
| `windows` | Running on Windows |
| `unix` | Running on Unix/Linux/macOS |
| `mono` | Running on Mono runtime |
| `dotNetCore` | Running on .NET Core |
| `administrator` | Running with admin/root privileges |
| `interactive` | Running in interactive mode |
| `network` | Network is available |
| `compileCSharp` | C# compilation is available |

### Using Constraints

```tcl
test windows-registry-1.1 {read registry value} -constraints {
    eagle windows
} -body {
    # Eagle uses .NET Registry classes instead of a built-in registry command
    set key [object invoke Microsoft.Win32.Registry OpenSubKey \
        "SOFTWARE\\Microsoft\\Windows\\CurrentVersion"]
    set value [object invoke $key GetValue "ProductName"]
    object invoke $key Close
    set value
} -cleanup {
    catch {object invoke $key Close}
    unset -nocomplain key value
} -match glob -result {Windows*}

test dotnet-compile-1.1 {compile C# code} -constraints {
    eagle compileCSharp
} -body {
    set code {
        public class Test {
            public static int Add(int a, int b) { return a + b; }
        }
    }
    compile -csharp $code
} -result {}
```

### Custom Constraints

```tcl
# Check and add a constraint
if {[haveConstraint myFeature]} {
    # Constraint already exists
} else {
    # Add based on condition
    addConstraint myFeature [expr {[info exists ::myFeatureEnabled]}]
}

# Use in test
test feature-1.1 {test my feature} -constraints {
    eagle myFeature
} -body {
    # Test code
}
```

---

## Testing .NET Integration

### Creating and Using Objects

```tcl
test object-create-1.1 {create StringBuilder} -body {
    set sb [object create System.Text.StringBuilder]
    object invoke $sb Append "Hello"
    object invoke $sb Append ", World!"
    object invoke $sb ToString
} -cleanup {
    unset -nocomplain sb
} -result {Hello, World!}
```

### Accessing Static Members

```tcl
test static-member-1.1 {get current time} -body {
    set now [object invoke System.DateTime Now]
    object invoke $now get_Year
} -match regexp -result {^\d{4}$}
```

### Proper Cleanup with object dispose

```tcl
test file-stream-1.1 {read file with stream} -setup {
    set tempFile [file tempname]
    writeFile $tempFile "test content"
} -body {
    set stream [object create System.IO.FileStream $tempFile Open Read]
    set reader [object create System.IO.StreamReader $stream]
    set content [object invoke $reader ReadToEnd]
    object dispose $reader
    object dispose $stream
    set content
} -cleanup {
    catch {object dispose $reader}
    catch {object dispose $stream}
    catch {file delete $tempFile}
    unset -nocomplain tempFile stream reader content
} -result {test content}
```

### Using BindingFlags for Non-Public Members

```tcl
test private-field-1.1 {access private field} -body {
    set obj [object create MyNamespace.MyClass]
    # Access private field
    object invoke -flags +NonPublic $obj privateField
} -cleanup {
    unset -nocomplain obj
} -result {expected value}
```

---

## Error Testing

### Testing for Expected Errors

```tcl
test error-divide-1.1 {division by zero} -body {
    expr {1 / 0}
} -returnCodes error -result {divide by zero}

test error-file-1.1 {file not found} -body {
    open /nonexistent/path/file.txt r
} -returnCodes error -match glob -result {*no such file*}

test error-argument-1.1 {invalid argument} -body {
    string length
} -returnCodes error -match glob -result {*wrong # args*}
```

### Testing Exception Types

```tcl
test exception-type-1.1 {ArgumentNullException} -body {
    # This should throw ArgumentNullException
    object invoke System.IO.Path GetFullPath $null
} -returnCodes error -match glob -result {*ArgumentNullException*}
```

---

## Output Testing

### Capturing Output

```tcl
test output-puts-1.1 {capture puts output} -body {
    set output [capture {
        puts "Hello, World!"
    }]
    string trim $output
} -result {Hello, World!}
```

### Using tputs for Test Output

```tcl
test logging-1.1 {test output logging} -body {
    tputs $test_channel "Test message\n"
    # tputs writes to both channel and log file
} -result {}
```

---

## Data-Driven Testing

### Parameterized Tests

```tcl
# Define test cases as a list of {input expected} pairs
set testCases {
    {"" 0}
    {"a" 1}
    {"hello" 5}
    {"hello world" 11}
}

set testNum 0
foreach {input expected} [join $testCases] {
    incr testNum
    test string-length-2.$testNum "string length of \"$input\"" -body {
        string length $input
    } -result $expected
}
unset -nocomplain testCases testNum input expected
```

### Using Key-Value Lists (NOT dict)

```tcl
# Eagle does NOT have dict - use key-value lists
test kvlist-1.1 {work with key-value list} -body {
    set data {name John age 30 city Boston}

    # Get value using getDictionaryValue (from auxiliary.eagle)
    set name [getDictionaryValue $data name ""]
    set age [getDictionaryValue $data age 0]
    set missing [getDictionaryValue $data country "USA"]

    list $name $age $missing
} -result {John 30 USA}
```

---

## Test Setup and Teardown Patterns

### Shared Setup with Variables

```tcl
test database-1.1 {insert and retrieve} -setup {
    set connection [openTestDatabase]
    set tableName "test_[clock seconds]"
    createTable $connection $tableName
} -body {
    insertRow $connection $tableName {name "Test" value 42}
    set result [selectRow $connection $tableName 1]
    getDictionaryValue $result name ""
} -cleanup {
    catch {dropTable $connection $tableName}
    catch {closeDatabase $connection}
    unset -nocomplain connection tableName result
} -result {Test}
```

### Using try/finally for Cleanup

```tcl
test resource-1.1 {ensure cleanup} -body {
    set resource [acquireResource]
    try {
        useResource $resource
        getResourceValue $resource
    } finally {
        releaseResource $resource
    }
} -result {expected value}
```

---

## Common Testing Patterns

### Testing Procedures

```tcl
# Define the procedure to test
proc add {a b} {
    expr {$a + $b}
}

test add-1.1 {add positive numbers} -body {
    add 2 3
} -result {5}

test add-1.2 {add negative numbers} -body {
    add -2 -3
} -result {-5}

test add-1.3 {add mixed numbers} -body {
    add -2 5
} -result {3}
```

### Testing with Temporary Files

```tcl
test tempfile-1.1 {write and read temp file} -setup {
    set tempFile [file tempname]
} -body {
    # Write content
    set f [open $tempFile w]
    puts $f "test content"
    close $f

    # Read content
    set f [open $tempFile r]
    set content [read $f]
    close $f

    string trim $content
} -cleanup {
    catch {file delete $tempFile}
    unset -nocomplain tempFile f content
} -result {test content}
```

### Testing Asynchronous Operations

```tcl
test async-1.1 {wait for async operation} -body {
    set completed false
    set result ""

    # Start async operation
    set asyncResult [startAsyncOperation {
        set completed true
        set result "done"
    }]

    # Wait with timeout
    set timeout 5000
    set start [clock milliseconds]
    while {!$completed && ([clock milliseconds] - $start) < $timeout} {
        update
        after 100
    }

    list $completed $result
} -result {true done}
```

### Testing External Processes with exec

Eagle's `[exec]` command provides extensive options for process execution in tests. Unlike Tcl's exec, Eagle does NOT support pipeline syntax (`|`) or I/O redirection operators (`<`, `>`, `2>&1`). Use Eagle's options instead.

**Basic exec usage:**

```tcl
test exec-basic-1.1 {run external command} -constraints {
    eagle
} -body {
    # Simple command - output is returned
    exec echo "Hello, World!"
} -match glob -result {Hello, World!*}

test exec-basic-1.2 {run command with arguments} -constraints {
    eagle
} -body {
    exec printf "%s %s" "arg1" "arg2"
} -result {arg1 arg2}
```

**Capturing exit codes:**

```tcl
test exec-exitcode-1.1 {capture exit code} -constraints {
    eagle
} -body {
    # Use -exitcode to capture the exit code in a variable
    exec -exitcode code -- [info nameofexecutable] -eval "exit 42"
    set code
} -result {42}

test exec-exitcode-1.2 {test for expected failure} -constraints {
    eagle
} -body {
    exec -exitcode code -- false
    expr {$code != 0}
} -result {1}
```

**Capturing stdout and stderr separately:**

```tcl
test exec-capture-1.1 {capture stdout and stderr} -constraints {
    eagle
} -body {
    # -stdout and -stderr store output in variables instead of returning
    exec -stdout out -stderr err -- sh -c "echo stdout; echo stderr >&2"
    list [string trim $out] [string trim $err]
} -result {stdout stderr}

test exec-capture-1.2 {ignore stderr} -constraints {
    eagle
} -body {
    # -ignorestderr prevents stderr from causing an error
    exec -ignorestderr -- sh -c "echo output; echo error >&2"
} -match glob -result {output*}
```

**Providing stdin input:**

```tcl
test exec-stdin-1.1 {provide stdin from variable} -constraints {
    eagle
} -body {
    set input "line1\nline2\nline3"
    # -stdin reads input content from the named variable
    exec -stdin input -- grep line2
} -match glob -result {line2*}

test exec-stdin-1.2 {pipe-like pattern with stdin} -constraints {
    eagle
} -body {
    # Simulating: echo "hello world" | tr a-z A-Z
    set data "hello world"
    exec -stdin data -- tr a-z A-Z
} -match glob -result {HELLO WORLD*}
```

**Working directory and timeout:**

```tcl
test exec-directory-1.1 {run in specific directory} -constraints {
    eagle
} -setup {
    set tempDir [file tempname]
    file mkdir $tempDir
} -body {
    # -directory sets the working directory for the child process
    exec -directory $tempDir -- pwd
} -cleanup {
    catch {file delete -force $tempDir}
    unset -nocomplain tempDir
} -match glob -result {*}

test exec-timeout-1.1 {timeout kills long-running process} -constraints {
    eagle
} -body {
    # -timeout specifies max milliseconds to wait
    catch {exec -timeout 1000 -- sleep 60} result
    string match "*timeout*" [string tolower $result]
} -result {1}
```

**Background execution:**

```tcl
test exec-background-1.1 {run process in background} -constraints {
    eagle
} -body {
    # Trailing & or -background runs without waiting
    exec -processid pid -background -- sleep 1
    expr {$pid > 0}
} -cleanup {
    catch {kill $pid}
    unset -nocomplain pid
} -result {1}
```

**Shell execution (opening documents/URLs):**

```tcl
test exec-shell-1.1 {shell execute document} -constraints {
    eagle windows
} -body {
    # -shell uses ShellExecute - opens with associated application
    # This would open notepad, so we just verify the option is accepted
    catch {exec -shell -timeout 100 notepad.exe} result
    # Process started (may timeout, but that's ok for this test)
    expr {1}
} -result {1}
```

**Windows-specific options:**

```tcl
test exec-windows-1.1 {hidden window} -constraints {
    eagle windows
} -body {
    # -windowstyle Hidden runs without visible window
    exec -windowstyle Hidden -exitcode code -- cmd /c "echo test"
    set code
} -result {0}

test exec-windows-1.2 {command processor execution} -constraints {
    eagle windows
} -body {
    # Use -commandline -forprocessor for proper cmd.exe escaping
    exec -commandline -forprocessor -- cmd /c "echo Hello World"
} -match glob -result {Hello World*}
```

**Unix/macOS-specific patterns:**

```tcl
test exec-unix-1.1 {shell command execution} -constraints {
    eagle unix
} -body {
    exec -commandline -- sh -c "echo Hello World"
} -match glob -result {Hello World*}
```

**Testing command output patterns:**

```tcl
test exec-pattern-1.1 {match output with glob} -constraints {
    eagle
} -body {
    exec -exitcode code -- [info nameofexecutable] -version
} -match glob -result {*Eagle*}

test exec-pattern-1.2 {match output with regexp} -constraints {
    eagle
} -body {
    exec date +%Y-%m-%d
} -match regexp -result {^\d{4}-\d{2}-\d{2}$}
```

**Error handling in exec tests:**

```tcl
test exec-error-1.1 {handle command failure} -constraints {
    eagle
} -body {
    # Commands that fail return error unless -exitcode captures it
    catch {exec false} result options
    getDictionaryValue $options -code ""
} -result {1}

test exec-error-1.2 {get all output on error} -constraints {
    eagle
} -body {
    # -setall ensures variables are set even on error
    catch {
        exec -setall -stdout out -stderr err -exitcode code -- \
            sh -c "echo out; echo err >&2; exit 1"
    }
    list [string trim $out] [string trim $err] $code
} -result {out err 1}
```

**Important exec notes for tests:**
- Always use `-exitcode varName` when testing programs that may return non-zero
- Use `-stdout` and `-stderr` to capture output separately
- Use `-stdin varName` instead of trying to pipe (`|`) input
- Use `-timeout` to prevent tests from hanging on stuck processes
- Use `-ignorestderr` when stderr output is expected but not an error
- Use `-directory` to control the working directory
- Use `-commandline` when arguments contain spaces or special characters
- Trailing `&` or `-background` for non-blocking execution

---

## Assertions and Verification

### Using expr for Assertions

```tcl
test assert-1.1 {verify condition} -body {
    set value 42
    expr {$value > 0 && $value < 100}
} -result {1}
```

### Pattern Matching with glob

```tcl
test glob-match-1.1 {match pattern} -body {
    set version [object invoke System.Environment get_Version]
    object invoke $version ToString
} -match glob -result {*.*.*.*}
```

### Pattern Matching with regexp

```tcl
test regexp-match-1.1 {match regex} -body {
    set guid [object invoke System.Guid NewGuid]
    object invoke $guid ToString
} -match regexp -result {^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$}
```

---

## Best Practices

### 1. Always Clean Up Resources

```tcl
test resource-cleanup-1.1 {proper cleanup} -setup {
    set obj [object create System.Text.StringBuilder]
} -body {
    object invoke $obj Append "test"
    object invoke $obj ToString
} -cleanup {
    # Always clean up in -cleanup block
    catch {unset obj}
} -result {test}
```

### 2. Use Constraints Appropriately

```tcl
# Don't run slow tests by default
test slow-operation-1.1 {performance test} -constraints {
    eagle slow
} -body {
    # Time-consuming operation
}
```

### 3. Make Tests Independent

Each test should be able to run in isolation:

```tcl
# BAD - depends on previous test
test bad-1.1 {first test} -body {
    set ::sharedVar "value"
}
test bad-1.2 {second test} -body {
    set ::sharedVar  ;# Fails if bad-1.1 didn't run
}

# GOOD - self-contained
test good-1.1 {independent test} -setup {
    set localVar "value"
} -body {
    set localVar
} -cleanup {
    unset -nocomplain localVar
} -result {value}
```

### 4. Use Descriptive Test Names and Descriptions

```tcl
# BAD
test t1 {test} -body { ... }

# GOOD
test string-split-empty-1.1 {splitting empty string returns empty list} -body {
    split "" ","
} -result {}
```

### 5. Test Edge Cases

```tcl
test edge-empty-1.1 {handle empty input} -body {
    myProc ""
} -result {}

test edge-null-1.1 {handle null input} -body {
    myProc $null
} -returnCodes error -match glob -result {*null*}

test edge-large-1.1 {handle large input} -constraints {
    eagle slow
} -body {
    set large [string repeat "x" 1000000]
    string length [myProc $large]
} -match regexp -result {^\d+$}
```

### 6. Don't Use `dict` - Use Key-Value Lists

```tcl
# BAD - dict doesn't exist in Eagle
test bad-dict-1.1 {using dict} -body {
    dict create name John age 30  ;# ERROR: invalid command name "dict"
}

# GOOD - use key-value lists
test good-kvlist-1.1 {using key-value list} -body {
    set data [list name John age 30]
    getDictionaryValue $data name ""
} -result {John}
```

---

## Quick Reference: Commonly Used Commands

### Test Framework

| Command | Description |
|---------|-------------|
| `test name desc ?options?` | Define a test |
| `runTestPrologue` | Initialize test environment |
| `runTestEpilogue` | Finalize test environment |
| `tputs channel string` | Write to test output |
| `tlog string` | Write to test log only |
| `addConstraint name ?value?` | Add a test constraint |
| `haveConstraint name` | Check if constraint exists |

### Object Operations

| Command | Description |
|---------|-------------|
| `object create type ?args?` | Create .NET object |
| `object invoke obj member ?args?` | Invoke member |
| `object dispose obj` | Dispose and release object |
| `object isoftype obj type` | Check object type |
| `object type obj` | Get object type |

### Utility Procedures

| Procedure | Description |
|-----------|-------------|
| `getDictionaryValue dict key ?default?` | Get value from key-value list |
| `isWindows` | Check if running on Windows |
| `isMono` | Check if running on Mono |
| `getEnvironmentVariable name` | Get environment variable |
| `writeFile path content` | Write string to file |
| `readFile path` | Read file contents |

---

## Example: Complete Test File

```tcl
###############################################################################
#
# myfeature.eagle -- Tests for MyFeature functionality
#
###############################################################################

package require Eagle
package require Eagle.Library
package require Eagle.Test

runTestPrologue

###############################################################################

# Test basic functionality
test myfeature-basic-1.1 {create instance} -body {
    set obj [object create MyNamespace.MyClass]
    object invoke $obj get_Name
} -cleanup {
    catch {unset obj}
} -result {DefaultName}

test myfeature-basic-1.2 {set and get property} -body {
    set obj [object create MyNamespace.MyClass]
    object invoke $obj set_Name "TestName"
    object invoke $obj get_Name
} -cleanup {
    catch {unset obj}
} -result {TestName}

###############################################################################

# Test error handling
test myfeature-error-1.1 {null argument throws} -body {
    set obj [object create MyNamespace.MyClass]
    object invoke $obj ProcessData $null
} -cleanup {
    catch {unset obj}
} -returnCodes error -match glob -result {*ArgumentNullException*}

###############################################################################

# Platform-specific tests
test myfeature-windows-1.1 {Windows-specific behavior} -constraints {
    eagle windows
} -body {
    set obj [object create MyNamespace.MyClass]
    object invoke $obj GetPlatformPath
} -cleanup {
    catch {unset obj}
} -match glob -result {C:\\*}

###############################################################################

runTestEpilogue
unset -nocomplain test_channel
```

---

## Troubleshooting

### Common Errors

| Error | Cause | Solution |
|-------|-------|----------|
| `invalid command name "dict"` | Using Tcl's dict command | Use key-value lists with `getDictionaryValue` |
| `wrong # args` | Incorrect argument count | Check command syntax in catalog |
| `object handle not found` | Object was disposed/released | Keep reference or recreate object |
| `type not found` | Assembly not loaded | Load assembly with `object load` |

### Debugging Tests

```tcl
test debug-1.1 {debugging example} -body {
    set value [computeValue]

    # Use tputs for debug output (visible in test log)
    tputs $test_channel "DEBUG: value = $value\n"

    set value
} -result {expected}
```

---

## Additional Resources

**Eagle Documentation Repository**: https://urn.to/r/docs (this will redirect)

| Document | Contents |
|----------|----------|
| `core_language.md` | Complete command reference, syntax, .NET integration, flags enumerations, command callbacks |
| `core_script_library.md` | Procedure library - test infrastructure, file operations, object utilities, platform detection |
| `README.md` | Documentation overview and index |

**Eagle Source Repository**: https://urn.to/r/github (this will redirect)
- Source code, examples, and test suites
- `Library/Tests/` - Comprehensive test examples

When in doubt, consult the documentation files for accurate syntax and usage examples.
