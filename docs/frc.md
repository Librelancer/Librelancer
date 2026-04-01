[TOC]
# FRC Compiler

The FRC format is a simple text-based format created by adoxa to represent resource DLL files. Librelancer provides support for compiling files in these format to .dll files.

## Compiling

`lleditscript -m compile-frc [-d] input output [--index=int]`

If `-d` is specified, input and output must be directories, otherwise they will be an frc file and dll file respectively.

If `index` is specified extra checks will be added to ensure that absolute numbers match the specifed index

Or find under **Scripts->Compile FRC** in LancerEdit.

## Indexing

String and infocard resources in .dll files have indices depending on where they are listed in the load order in freelancer.ini, with resources.dll hardcoded at position 0. The first .dll file contains indices 0-65535, the second 65536+ etc.

Passing the index parameter to the FRC compiler will enable error checking to make sure each ID in the file is within the correct range for the load order. Otherwise, the compiler only takes the bottom 16 bits of each ID, wrapping the ID within the 0-65535 range in the file.

## Syntax

.frc files consist of single character commands "S" and "H" to define resources. These commands should appear as the first character on the line.

The text of a command may immediately follow it, or start on the next line. The text continues until another command is found (a character that is not a space at the beginning of a line). Leading spaces are ignored, trailing spaces are preserved.

Example:

```
S 123
 This is one line of text.
 And this is the second line.
```

Use a backslash ("\") to continue the same line of text, otherwise a new line is started. A backslash will otherwise quote the next character (use for leading space), or form an escape sequence. Finish a string with "\." to show trailing spaces.

Escape Sequences:

| Sequence | Character              |
|----------|------------------------|
| `\0`     | ⁰                      |
| `\1`     | ₁                      |
| `\2`     | ₂                      |
| `\3`     | ₃                      |
| `\4`     | ⁴                      |
| `\5`     | ⁵                      |
| `\6`     | ⁶                      |
| `\7`     | ⁷                      |
| `\8`     | ⁸                      |
| `\9`     | ⁹                      |
| `\uXXXX` | Unicode codepoint XXXX |

### S

`S id [~] string` e.g. `S 123 Hello World`

Adds a string to the .dll file with the specified ID. Add `~` to switch to rich text mode.

### H

`H id [~] rich text` e.g. `H 123 \b{HELLO}`

Adds a rich text infocard to the .dll file with the specified ID. Add `~` to interpret as raw text, instead of escaped rich text. `I` is also an alias to this command in Librelancer.

Rich text can be formatted using the following escape sequences:

| Sequence   | RDL                       | Notes                  |
|------------|---------------------------|------------------------|
| `\b`       | `<TRA bold="true">`       |                        |
| `\B`       | `<TRA bold="false">`      |                        |
| `\i`       | `<TRA italic="true"/>`    |                        |
| `\I`       | `<TRA italic="false"/>`   |                        |
| `\u`       | `<TRA underline="true"/>` |                        |
| `\U`       | `<TRA underline="false"/>`|                        |
| `\cC`      |  `<TRA bold="#RRGGBB"/>`  | Must be lower case     |
| `\cRRGGBB` | `<TRA color="#RRGGBB">`   | Must be upper case     |
| `\cName`   | `<TRA color="#RRGGBB">`   | See color table below  |
| `\fN`      | `<TRA font="N">`          | One or two digits      |
| `\F`       | `<TRA font="default">`    |                        |
| `\hN`      | `<POS h="N" relH="true"/>`| One to three digits    |
| `\l`       | `<JUST loc="l">`          | Align paragraph left   |
| `\m`       | `<JUST loc="c">`          | Align paragraph middle |
| `\r`       | `<JUST loc="r">`          | Align paragraph right  |
| `\n`       | `<PARA/>`                 | New paragraph          |

As an alternative to the inverse (upper case) formatting, format tags may be enclosed in braces. `\b{Text}` is equivalent to `\bText\B`.

To include XML, escape the opening tag with `\<`, otherwise XML unsafe characters `&<>` will be replaced with `&amp;`, `&lt;` and `&gt;` respectively.

### L

Ignored. Left in for compatibility with adoxa frc, we only output 1033 as the language ID.

### Comments

Comments start with a semicolon `;`, either at the beginning of a line or after a space (e.g. `hello; there` will not create a comment).

Multiline comments may also be created by starting a line with `;+` and ending with `;-`. Multiline comments may not start mid-line.

_Examples:_

```
; This is a comment

S 123 This is; not commented

S 456 This is a string ; and this is a comment after it

;+
You can write a very long comment
within a block!
;-
```

## Differences with adoxa FRC

- Librelancer FRC will always create a completely new .dll file, and not update an existing one. Dll version metadata will be removed, as well as any data that is not part of the text .frc file

- Librelancer assumes UTF-8 text encoding for .frc files, while adoxa .frc assumes Windows-1252 if the UTF-16 BOM is not found. Where possible, Librelancer will try to detect files in Windows-1252 encoding.
