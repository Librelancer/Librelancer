# Locating Log Files

Log files for LancerEdit and the Librelancer engine may be found in the following locations:

| Platform | Location |
|----------|----------|
|Windows|`%LocalAppData%\Librelancer\logs`|
|Linux|`$XDG_STATE_HOME/Librelancer/logs` (usually `~/.local/state/Librelancer/logs`)|

*NOTE:* lleditscript does not produce log files outside of console output.
Redirect lleditscript output to a file if needed.
