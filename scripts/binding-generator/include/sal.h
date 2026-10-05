/* Parsing-only C runtime header of the binding generator (docs/adr/0026). SDL3 uses these MSVC
   source annotations; they carry no type information. */
#ifndef JADE_SAL_H
#define JADE_SAL_H

#define _In_bytecount_(size)
#define _Inout_z_cap_(size)
#define _Out_z_cap_(size)
#define _Out_cap_(size)
#define _Out_bytecap_(size)
#define _Out_z_bytecap_(size)
#define _Printf_format_string_
#define _Scanf_format_string_impl_

#endif
