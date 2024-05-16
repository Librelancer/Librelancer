# Librelancer Scripts

The Librelancer SDK supports running C# scripts linked to the engine libraries. These can help automate tasks for editing your mod's files.

**WARNING: Do not run scripts you don't trust. They can execute any arbitrary code on your system**

Each script is a C# file with the extension `.cs-script`. It is a top-level program, meaning you are already in the scope of your `Main()` method. Arguments passed to the script are accessible through the global string array `Arguments`

Scripts may be run in one of two ways.

## Command Line

Simply run `lleditscript script.cs-script [arguments]` to use the command line runner for Librelancer scripts.


## Scripts menu of LancerEdit

Scripts accessible in the `Scripts` menu of LancerEdit must be placed in an `editorscripts` folder next to the LancerEdit binary

To describe arguments, use an ini syntax in comment lines at the very start of the script. Each script must have a name defined

```
// [Script]
// name = My fancy script
```

Arguments are defined with their own sections, and are passed to the script in the order in which they are defined.

```
// [Argument]
// name = my argument
// type = string
```

Valid types for arguments include: `string`, `integer`, `boolean`, `dropdown`, `file`, `folder` and `filearray`

`dropdown` arguments require a list of `option` keys afterwards. The first item defined as an option will be selected by default.

```
// [Argument]
// name = fruit
// type = dropdown
// option = apple
// option = banana
// option = blueberry
```

`filearray` arguments pass each file as a separate argument, and thus are required to be at the end of the list of defined Arguments to be useful.

Please see the built-in script files in `lib/editorscripts/` as working examples.

See the [API Reference](api/reference.md) for further details.
