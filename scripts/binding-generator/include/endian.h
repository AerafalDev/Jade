/* Parsing-only C runtime header of the binding generator (docs/adr/0026). SDL3 reads the byte order
   from it on Linux and Android. */
#ifndef JADE_ENDIAN_H
#define JADE_ENDIAN_H

#define __LITTLE_ENDIAN __ORDER_LITTLE_ENDIAN__
#define __BIG_ENDIAN __ORDER_BIG_ENDIAN__
#define __BYTE_ORDER __BYTE_ORDER__

#endif
