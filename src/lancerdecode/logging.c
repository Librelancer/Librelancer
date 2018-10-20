// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

#include "lancerdecode.h"
#include "logging.h"
#include <stdarg.h>
#include <stdio.h>
ld_errorlog_callback_t callback;

LDEXPORT void ld_errorlog_register(ld_errorlog_callback_t cb)
{
	callback = cb;
}

void ld_logerrorf(const char *fmt, ...)
{
	if(!callback)
		return;
	const char buffer[1024];
	va_list args;
	va_start(args, fmt);
	vsnprintf(buffer,1024,fmt,args);
	callback((const char*)buffer);
	va_end(args);
}
