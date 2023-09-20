# Build-time StarKid options

## `StarKid_Help_MaxCharsPerLine` -- Help text maximum column length

Property name:
    `StarKid_Help_MaxCharsPerLine`

Purpose:
    Sets the maximum number of characters allowed in each line of the
    help text before starting a new line. Might not be exactly
    respected when the constraint is impossible to solve, i.e. there
    is not enough space to display the minimal amount of text
    required.

Allowed values:
    Any positive integer above 40, or -1 to disable the limit
    entirely.

Default value: 80 characters

Example usage:
```xml
<PropertyGroup>
    <StarKid_Help_MaxCharsPerLine>120</StarKid_Help_MaxCharsPerLine>
</PropertyGroup>
```

## `StarKid_Help_ExitCode` -- Exit code for `--help` option

Property name:
    `StarKid_Help_ExitCode`

Purpose:
    Sets the exit code returned when a user requests the build-in
    `--help`/`-h` option of a command or group.

Allowed values:
    Any integer.

Default value: 1

Example usage:
```xml
<PropertyGroup>
    <StarKid_Help_ExitCode>41</StarKid_Help_ExitCode>
</PropertyGroup>
```

## `StarKid_AllowRepeatingOptions` -- Don't complain about repeated options in command lines

Property name:
    `StarKid_AllowRepeatingOptions`

Purpose:
    By default, the generated parser will emit an error when a user
    tries to specify an option or flag multiple times. When set to
    `true`, this property makes the parser ignore such a situation
    and simply use the last specified value. So `./my-app --foo 1 --foo 2`
    would mean that the final value for `foo` would be `2`.

Allowed values:
    `true` or `false`.

Default value: false

Example usage:
```xml
<PropertyGroup>
    <StarKid_AllowRepeatingOptions>true</StarKid_AllowRepeatingOptions>
</PropertyGroup>
```