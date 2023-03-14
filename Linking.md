# Installation
Install MinGW to `C:\MinGW`

In MinGW installation manager, install `mingw-32-base` bin.

Install NASM to `C:\MinGW\bin`

Go to `Advanced System Settings` on Windows.

Click on `Environmental Variables`

Select `Path` under `User Variables` and click the `Edit` button

Click on `New` then write `C:\MinGW\bin`

Check if it works by opening cmd and running `nasm --version` and `gcc --version`

## Assembling and Compiling

Create a file called helloworld.asm

```asm
global    _main                
extern    _printf              

segment  .data
	message: db   'Hello world', 10, 0

section .text
_main:                            
        push    message           
        call    _printf 
        add     esp, 4           
        ret 
```

cd to where you saved the asm file

Assemble to COFF obj using 
```bash
nasm -f win32 helloworld.asm
```

Compile with 
```bash
gcc helloworld.obj -o helloworld.exe
```

Run the exe from cmd to check if it works

![image](https://user-images.githubusercontent.com/86862094/225109373-cdb62ea6-c2c7-43f8-8c1e-5c8bc955329e.png)
