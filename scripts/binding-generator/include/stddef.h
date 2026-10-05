/* Parsing-only C runtime header of the binding generator (docs/adr/0026). Types come from clang's
   predefined macros, so they match the target triple without any system or builtin header. */
#ifndef JADE_STDDEF_H
#define JADE_STDDEF_H

typedef __SIZE_TYPE__ size_t;
typedef __PTRDIFF_TYPE__ ptrdiff_t;
typedef __WCHAR_TYPE__ wchar_t;

#define NULL ((void *)0)
#define offsetof(type, member) __builtin_offsetof(type, member)

#endif
