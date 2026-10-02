/* Parsing stub for scripts/generate-bindings.cs, see stddef.h. The Apple platform comes from the target triple. */
#ifndef JADE_STUB_TARGETCONDITIONALS_H
#define JADE_STUB_TARGETCONDITIONALS_H
#define TARGET_OS_MAC 1
#define TARGET_OS_OSX __is_target_os(macos)
#define TARGET_OS_IPHONE (__is_target_os(ios) || __is_target_os(tvos) || __is_target_os(watchos) || __is_target_os(xros))
#define TARGET_OS_IOS __is_target_os(ios)
#define TARGET_OS_MACCATALYST __is_target_environment(macabi)
#define TARGET_OS_TV __is_target_os(tvos)
#define TARGET_OS_WATCH __is_target_os(watchos)
#define TARGET_OS_VISION __is_target_os(xros)
#define TARGET_OS_SIMULATOR __is_target_environment(simulator)
#define TARGET_OS_EMBEDDED (TARGET_OS_IPHONE && !TARGET_OS_SIMULATOR)
#define TARGET_CPU_X86_64 __x86_64__
#define TARGET_CPU_ARM64 __aarch64__
#endif
