#ifndef _EMBEDDING_H_
#define _EMBEDDING_H_
#ifdef MSABI
#define EMBED __attribute__((ms_abi))
#elif defined SYSVABI
#define EMBED __attribute__((sysv_abi))
#else
#define EMBED
#endif
#endif