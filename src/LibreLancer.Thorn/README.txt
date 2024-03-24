This project only implements a small subset of the Lua 3.2 VM

The following features are excluded intentionally:
- File I/O
- Debugging
- Tag Methods


This is to protect the engine from maliciously crafted bytecode.
As the thorn VM is only used for creating objects for cutscenes, full general programming features are not necessary.
