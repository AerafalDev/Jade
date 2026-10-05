/*
 * The entries that a layout library reports, and the macros its generated source writes them with
 * (docs/adr/0036). The binding generator writes one source per interop library next to this file;
 * the C# layout tests of the library read its table and compare it with the C# layouts.
 */
#ifndef JADE_LAYOUT_H
#define JADE_LAYOUT_H

#include <stddef.h>

#ifdef __cplusplus
extern "C" {
#endif

/*
 * The library hides every symbol but the function that returns its table, which the tests resolve
 * by name.
 */
#if defined(_WIN32)
#define JADE_LAYOUT_API __declspec(dllexport)
#else
#define JADE_LAYOUT_API __attribute__((visibility("default")))
#endif

/*
 * The platform families of docs/adr/0012, as the binding generator gives a declaration its
 * availability: a record available on some families only is reported there only. Android defines
 * __linux__ too, and an iOS simulator defines TARGET_OS_IOS.
 */
#if defined(__EMSCRIPTEN__)
#define JADE_LAYOUT_BROWSER
#elif defined(_WIN32)
#define JADE_LAYOUT_WINDOWS
#elif defined(__ANDROID__)
#define JADE_LAYOUT_ANDROID
#elif defined(__linux__)
#define JADE_LAYOUT_LINUX
#elif defined(__APPLE__)
#include <TargetConditionals.h>
#if TARGET_OS_IOS
#define JADE_LAYOUT_IOS
#elif TARGET_OS_OSX
#define JADE_LAYOUT_MACOS
#endif
#endif

#if !defined(JADE_LAYOUT_BROWSER) && !defined(JADE_LAYOUT_WINDOWS) && !defined(JADE_LAYOUT_ANDROID) \
    && !defined(JADE_LAYOUT_LINUX) && !defined(JADE_LAYOUT_IOS) && !defined(JADE_LAYOUT_MACOS)
#error "The layout libraries do not know this platform: add it with its family of docs/adr/0012."
#endif

/*
 * A record or a member: a record is named after its C name and reports its size and alignment; a
 * member is named "record.member", or "record.member[0]" for the first element of an array, and
 * reports its size and its offset from the start of its record. The table ends with an entry
 * whose name is NULL.
 */
typedef struct jade_layout_entry {
    const char* name;
    size_t size;
    size_t alignment;
    size_t offset;
} jade_layout_entry;

#define JADE_LAYOUT_RECORD(name, type) { name, sizeof(type), _Alignof(type), 0 }
#define JADE_LAYOUT_MEMBER(name, type, member) { name, sizeof(((type*)0)->member), 0, offsetof(type, member) }

/*
 * An anonymous record has no type name, so it is reached through the members that lead to it from
 * the named record that holds it. C11 cannot take the alignment of an expression's type: the
 * alignment of an anonymous record is reported as 0, and the offsets and sizes of the records that
 * hold it depend on it anyway.
 */
#define JADE_LAYOUT_NESTED_RECORD(name, type, path) { name, sizeof(((type*)0)->path), 0, 0 }
#define JADE_LAYOUT_NESTED_MEMBER(name, type, path, member) \
    { name, sizeof(((type*)0)->path.member), 0, offsetof(type, path.member) - offsetof(type, path) }

#define JADE_LAYOUT_END { NULL, 0, 0, 0 }

#ifdef __cplusplus
}
#endif

#endif
