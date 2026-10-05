/* Parsing-only C runtime header of the binding generator (docs/adr/0026). SDL3 checks the macOS
   deployment target, which clang derives from the triple. */
#ifndef JADE_AVAILABILITYMACROS_H
#define JADE_AVAILABILITYMACROS_H

#define MAC_OS_X_VERSION_MIN_REQUIRED __ENVIRONMENT_MAC_OS_X_VERSION_MIN_REQUIRED__

#endif
