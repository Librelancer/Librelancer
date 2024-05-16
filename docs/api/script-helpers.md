<!-- API -->
# Script Helpers

These methods are available within any lleditscript program
Functions for specifying options should all be called _before_ ParseArguments.

```csharp
// Helper functions

// Option parsing
// Boolean option for setting a flag
void FlagOption(string prototype, string description, Action<bool> action);

// Example:

    bool verbose = false;
    FlagOption("v|verbose", "Verbose logging", v => verbose = v);

//Integer option
//Allows setting a value with --option=123
void IntegerOption(string prototype, string description, Action<int> action)

// Example:
    int count = 0;
    IntegerOption("count=", "Count", v => count = v);

//String option
//Allows setting a value with --option="abc"
void StringOption(string prototype, string description, Action<string> action)

// Example:
    string name = "";
    StringOption("name=", "Name of thing", v => name = v);

// ScriptUsage
// Sets the usage message printed when there are less than min args specified
void ScriptUsage(string usage);

// ParseArguments
// Performs all the option parsing as specified with the *Option functions,
// and then returns an array of all left over arguments. Optionally prints usage and exits
// when there are less than minimum arguments.
string[] ParseArguments(int minArgs = 0);


// Check if file exists, and exit with error code 2 if it does not
void AssertFileExists(string filename);
void AssertFilesExist(IEnumerable<string> filenames);

```

