# Build-time Recline options

## `ReclineHelpColumnLength` -- Help text maximum column length

Property name:
    `ReclineHelpColumnLength`

Purpose:
    Sets the maximum number of characters allowed in each line of the
    help text before starting a new line. Might not be exactly
    respected when the constraint is impossible to solve, i.e. there
    is not enough space to display the minimal amount of text
    required.

Allowed values:
    Any positive integer above 20, or -1 to disable the limit
    entirely.

Default value: 80 characters

Example usage:
```xml
<PropertyGroup>
    <ReclineHelpColumnLength>120</ReclineHelpColumnLength>
</PropertyGroup>
```

## `ReclineHelpExitCode` -- Exit code for `--help` option

Property name:
    `ReclineHelpExitCode`

Purpose:
    Sets the exit code returned when a user requests the build-in
    `--help`/`-h` option of a command or group.

Allowed values:
    Any integer.

Default value: 1

Example usage:
```xml
<PropertyGroup>
    <ReclineHelpExitCode>41</ReclineHelpExitCode>
</PropertyGroup>
```