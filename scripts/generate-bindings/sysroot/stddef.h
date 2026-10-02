/* Parsing stub for scripts/generate-bindings.cs: the generator parses with -nostdinc so every target
 * triple parses the same way without its SDK. Types come from clang's per-target predefined macros. */
#ifndef JADE_STUB_STDDEF_H
#define JADE_STUB_STDDEF_H
typedef __SIZE_TYPE__ size_t;
typedef __PTRDIFF_TYPE__ ptrdiff_t;
#ifndef __cplusplus
typedef __WCHAR_TYPE__ wchar_t;
#endif
#define NULL ((void *)0)
#define offsetof(type, member) __builtin_offsetof(type, member)
#endif
