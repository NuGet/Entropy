# OpenSSL build instructions

The paths are examples.

1. Create build directory, e.g. `D:\src\openssl`.

1. Download and extract [OpenSSL 1.0.2k source](https://www.openssl.org/source/old/1.0.2/openssl-1.0.2k.tar.gz), e.g. `D:\src\openssl\openssl-1.0.2k`.

1. Create a build output directory, e.g. `D:\src\openssl\openssl-1.0.2k-x64`.

1. Download and install [ActivePerl 5.24.2.2403](http://downloads.activestate.com/ActivePerl/releases/5.24.2.2403/ActivePerl-5.24.2.2403-MSWin32-x64-403863.exe). Perl should be in the `PATH`.

1. Open x64 Native Tools Command Prompt for VS 2017.

1. `cd D:\src\openssl\openssl-1.0.2k`

1. `perl Configure VC-WIN64A --prefix=D:\src\openssl\openssl-1.0.2k-x64`

1. `ms\do_win64a`

1. `nmake -f ms\nt.mak`

1. `nmake -f ms\nt.mak install`

1. The output is in `D:\src\openssl\openssl-1.0.2k-x64\bin`.
