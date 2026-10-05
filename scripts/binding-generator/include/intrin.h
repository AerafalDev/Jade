/* Parsing-only C runtime header of the binding generator (docs/adr/0026). SDL3 calls this MSVC
   intrinsic in an inline function. */
#ifndef JADE_INTRIN_H
#define JADE_INTRIN_H

unsigned char _BitScanReverse(unsigned long *index, unsigned long mask);

#endif
