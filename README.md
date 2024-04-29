# Glory
A compiler front-end and back-end built from scratch in 1 month.

> [!NOTE]
>Please bear in mind Glory is a proof-of-concept, rather than a language that is intended to be used.
>There are no intentions of updating Glory at the moment.

# Windows Installation
Glory currently only supports NASM output for Windows 10. This means an external assembler and linker need to be installed onto your system to use Glory:

- Install MinGW to `C:\MinGW`.

- In MinGW installation manager, install `mingw-32-base` bin.

![image](https://github.com/lxkast/Glory/assets/86862094/88aa8ecc-a77f-4e58-ad97-5729216f3d8c)

- Hit `Apply Changes`

![image](https://github.com/lxkast/Glory/assets/86862094/5b08cc48-5364-4d20-b304-a3ed8cb46516)


- Install NASM to `C:\MinGW\bin`.

- Go to `Advanced System Settings` on Windows.

- Click on `Environmental Variables`.

![image](https://github.com/lxkast/Glory/assets/86862094/fa31fd78-4822-4abe-9a5a-f844265e89f3)

- Select `Path` under `User Variables` and click the `Edit` button.

- Click on `New` then write `C:\MinGW\bin`.

- Check if it works by opening cmd and running `nasm --version` and `gcc --version`.

## Assembling

- Create a file called helloworld.asm.

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

- Cd to where you saved the asm file.

- Assemble using 
```bash
nasm -f win32 helloworld.asm
```

- Link with 
```bash
gcc helloworld.obj -o helloworld.exe
```

- Run the exe from command prompt and check if it works.

![image](https://user-images.githubusercontent.com/86862094/225109373-cdb62ea6-c2c7-43f8-8c1e-5c8bc955329e.png)

## Glory Installation

Once the assembler and linker is setup on your system, you can install Glory from the `Releases` section of the repository.

It is recommended to add `glorycompiler.exe` as an environment variable, as this allows you to run the compiler from any directory.

# Usage
- Create a new file ending with a `.glr` file extension.

![image](https://github.com/lxkast/Glory/assets/86862094/3e70945b-6974-4317-b5d5-25949988f500)

![image](https://github.com/lxkast/Glory/assets/86862094/6d418539-bdc5-4c24-9d49-e45ff1be80b7)

- Run the compiler using:
```bash
glorycompiler [path to glr file]
```

![image](https://github.com/lxkast/Glory/assets/86862094/3657c696-a1ce-4a6b-a842-803874067837)

- Run the compiled EXE through the command prompt.

![image](https://github.com/lxkast/Glory/assets/86862094/49a23e41-917a-422f-b509-7a2f7fc6db22)

# Features
- Multidimensional arrays
- Function call indexing
- Dead code elimination
- Recursion
- Order of operations (`1 + 2 * 3` is treated as `1 + (2 * 3)`)
- While loops

# Limitations
- No floats, strings or other data types/structures
- No exponentiation operator
- No bound checking for arrays
- No array literals
- Only able to print integers using `printInt()`

# Example programs
## Iterative Factorial
```python
int factorial(int n)
{
    int answer = 1;
    while n > 0
    {
        answer *= n;
        n -=1 ;
    }
    return answer;
}
printInt(factorial(6));
```
`Output: 720` 

## Recursive Factorial
```python
int factorial(int n)
{
	if n == 0
	{
		return 1;
	}
	else
	{
		return n * factorial(n-1);
	}
}

printInt(factorial(5));
```
`Output: 120`

### Bubble Sort
```python
int[8] sort(int[8] arr)
{
    int i = 0;
    while i < 8
    {
        int j = 0;
        while j < 8 - i - 1
        {
            if arr[j] > arr[j + 1]
            {
                int temp = arr[j];
                arr[j] = arr[j + 1];
                arr[j + 1] = temp;
            }
            j += 1;
        }
        i += 1;
    }
    return arr;
}

int[8] myArr; # = [10,2,5,3,0,10,120,25];
# The pain of having no array literals
myArr[0] = 10; myArr[1] = 2; myArr[2] = 5; myArr[3] = 3;
myArr[4] = 0; myArr[5] = 10; myArr[6] = 120; myArr[7] = 25;

int[8] sorted = sort(myArr);

int i = 0;
while(i < 8)
{
    printInt(sorted[i]);
    i+= 1;
}
```
`Output: 0235101025120`
