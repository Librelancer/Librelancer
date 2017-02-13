Shaders in the Librelancer engine are a little dodgy due to being implemented as a GL 3.2 / GLES 2 hybrid

Requirements:
- Shaders must be version 140 or 150 (either works)
- Texture accesses must be through the texture() function
- Loops must have a constant max number of iterations (see lighting.inc for an example)
- Uniform Buffer Objects are std140 only and must be implemented with BEGIN_UNIFORMBUFFEROBJECT(name) and END_UNIFORMBUFFEROBJECT

Probably if someone is ever feeling up to the task, a better shader syntax should be created.