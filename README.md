# Glory
A custom-made "written in 1 month" compiler for a custom-made "written in 1 day" language.

# Windows Installation
Glory currently only supports NASM output for Windows 10. This means an external assembler and linker need to be installed onto your system to use Glory:

- Install MinGW to `C:\MinGW`.

- In MinGW installation manager, install `mingw-32-base` bin.

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

## Glory installation

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

# Documentation

The website for docs is currently still under development.
![image](https://github.com/lxkast/Glory/assets/86862094/fbac9e06-7980-4e83-906e-353ca19e4a29)

Looks great I know.

Until the website is complete, here are a few notes detailing Glory's syntax.

## Syntax

- Glory's syntax is **C-like**, meaning blocks of code are denoted by curly braces `{ }` and statements end with a semicolon `;`.
- Functions are also syntatically equivalent to C, except the void keyword is replaced with the `blank` keyword.
- Conditional statements, such as while loops and if statements, do not require parentheses surrounding the conditional statement.
- All functions must be defined previously in the program to be able to use them. (And no, you currently cannot forward declare functions in Glory. Too bad!)
- The compiler understands order of operations. `1 + 2 * 3` is treated the same as `1 + (2 * 3)`.
- Glory supports compound assignment operators (`+=`, `-=`, `*=` etc).
- The power operation is represented by the `^` symbol.
- Glory supports `elif` statements.
- To make an array in glory, define the type of the items, with square brackets with an integer value, followed by the identifier. For example `int[5] arr;`. The array size must be an integer literal, as it must be known at compile time.
- Glory doesn't yet support for loops (will add soon), array literals, strings, floats or dynamically allocated variables. These features are all planned to be implemented sometime.
- Glory allows for multidimensional arrays. They can be declared as `int[a][b] arr;` where a is the number of items in each list, and b being the number of lists.
- Indexing a multidimensional array can be thought of as the inverse of the declaration. Where the `[]`is indexing the outer array, then the next is indexing one deeper.
- Functions that return an array can be indexed on the call. For example a function `dProduct` that returns an array can be used like `dProduct()[1]`.
- Glory does not do bounds checking for arrays. If an array is 5 items large and you try to index the 8th item, undefined behaviour will occur.
- You can only print integers to the console, through the `printInt()` native function.
## Example programs
### Iterative Factorial
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

Please bear in mind Glory is a proof-of-concept, rather than a language that is intended to be used.
There's a lot of bugs, things that need fixing, and cases that haven't yet been accounted for, so if you find anything, please leave an issue or a pull request!
