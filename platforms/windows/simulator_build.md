# Windows Simulator Build

This documents the exact steps to build `lvgl_simulator_host.exe` with a real
SDL2/LVGL window (`LVGL_SIMULATOR_WITH_RUNTIME=ON`) on Windows x64.

## Prerequisites

### 1. MSYS2 with MinGW64 toolchain

Install MSYS2 from https://www.msys2.org/ and run in a MinGW64 shell:

```bash
pacman -S --noconfirm \
  mingw-w64-x86_64-gcc \
  mingw-w64-x86_64-cmake \
  mingw-w64-x86_64-ninja \
  mingw-w64-x86_64-SDL2
```

### 2. LVGL 9.4 source in `third_party/lvgl-9.4`

The directory `third_party/lvgl-9.4` must contain a full LVGL 9.4.0 checkout.
If it is missing or empty, clone it once:

```bash
git clone --depth 1 --branch v9.4.0 \
  https://github.com/lvgl/lvgl.git \
  third_party/lvgl-9.4
```

Verify: `third_party/lvgl-9.4/lvgl.h` must exist.

## Build Steps

All cmake commands must be run inside the **MSYS2 MinGW64 shell** (`mingw64.exe`),
not in Git Bash or PowerShell, because `g++.exe` requires the MSYS2 runtime to be
on PATH.

### Configure

```bash
cmake -S native/lvgl_simulator_host \
      -B native/lvgl_simulator_host/build-windows \
      -G Ninja \
      -DCMAKE_C_COMPILER="C:/msys64/mingw64/bin/gcc.exe" \
      -DCMAKE_CXX_COMPILER="C:/msys64/mingw64/bin/g++.exe" \
      -DCMAKE_MAKE_PROGRAM="C:/msys64/mingw64/bin/ninja.exe" \
      -DLVGL_SIMULATOR_WITH_RUNTIME=ON
```

Expected cmake output (key lines):

```
-- SDL2 configured: 1
-- LVGL headers configured: 1
-- Configuring done
-- Generating done
```

### Build

```bash
cmake --build native/lvgl_simulator_host/build-windows
```

## Output

The build produces:

```
native/lvgl_simulator_host/build-windows/lvgl_simulator_host.exe
```

## Deploy to simulator directory

Copy the new binary to the platform release folder:

```bash
cp native/lvgl_simulator_host/build-windows/lvgl_simulator_host.exe \
   platforms/windows/simulator/lvgl_simulator_host.exe
```

The following DLLs must be present next to the exe. They are not rebuilt by
the cmake build and must be copied manually from `C:/msys64/mingw64/bin/`
if missing or if the MinGW toolchain version changes:

- `platforms/windows/simulator/SDL2.dll`
- `platforms/windows/simulator/libgcc_s_seh-1.dll`
- `platforms/windows/simulator/libstdc++-6.dll`
- `platforms/windows/simulator/libwinpthread-1.dll`

`SDL2.dll` is required because the runtime build links against SDL2 dynamically.
It was not needed for the old stub build (`LVGL_SIMULATOR_WITH_RUNTIME=OFF`).

## Known Issue: LVGL 9.4.0 on MinGW — `lv_fs_stdio.c`

LVGL 9.4.0 has a build error in `src/libs/fsdrv/lv_fs_stdio.c` when compiled
with MinGW on Windows. The file mixes the Windows `HANDLE` type with POSIX
`DIR`/`readdir` calls, which does not compile cleanly under MinGW.

**Fix applied in `native/lvgl_simulator_host/config/lv_conf.h`:**

```c
#define LV_USE_FS_STDIO 0
```

The simulator does not need LVGL filesystem access. All screen content comes
from the generated C code, not from files loaded at runtime through LVGL.

## Why cmake must run inside MSYS2

Running cmake from Git Bash or PowerShell causes the CXX compiler check to
fail silently. The MSYS2 `g++.exe` depends on `msys-2.0.dll` being on PATH,
which is only the case inside a proper MSYS2 MinGW64 shell session.
