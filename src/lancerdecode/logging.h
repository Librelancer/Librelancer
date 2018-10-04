#ifndef _LOGGING_H_
#define _LOGGING_H_

#ifndef LOG_ERROR
#define LOG_ERROR_F(x, ...) ld_logerrorf(x, __VA_ARGS__);
#define LOG_ERROR(x) ld_logerrorf(x);
#endif

void ld_logerrorf(const char *fmt, ...);
#endif 
