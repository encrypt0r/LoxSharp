# LoxSharp
Lox is a little programming language created by [Bob Nystrom](https://twitter.com/intent/user?screen_name=munificentbob) in his book [Crafting Interpreters](http://www.craftinginterpreters.com/). This is my attempt at recreating it in C#. I've published a web based version of the interpreter [here](https://mhmd-azeez.github.io/LoxSharp/).

## What's implemented
The interprer is still a work in progress as I've not finished the book yet.

### Operators

| Name           | Operators | Associates |
|----------------|-----------|------------|
| Unary          | ! -       | Right      |
| Multiplication | / * %     | Left       |
| Addition       | - +       | Left       |
| Comparison     | > >= < <= | Left       |
| Equality       | == !=     | Left       |

### Variables
Lox is a dynamically typed language, variables can have values of four types: `nil`, `number` (64-bit floating point numbers), `boolean` and `string`.

```kotlin
var x = 5.8;
var y;
y = 10;
var z = x * y;
var i = true;
print(i);
print(z);
```

### If conditional
```kotlin
var x = 250;
if (x % 2 == 0)
{
    print("Even");
}
else
{
    print("Odd");
}
```
### Loops

#### While loop
```kotlin
var x = 0;
while(x < 10)
{
    if (x > 5)
        break;
        
    print(x);
    x = x + 1;
}
```
#### For loop
```kotlin
for (var i = 0; i < 10; i = i + 1)
{
    print("i is: " + i);
}
```
### Functions
Functions can return something:
```kotlin
fun coolify(thing)
{
    return "cool " + thing;
}

print(coolify("app"));
```

Or return nothing:

```kotlin
fun doSomething() {
    print("Doing something...");
}

doSomething();
```

### Standard Library
There are only two functions in the standard library:
#### Print
pPrints the value to stdout:
```kotlin
print("Hello World!");
print(42);
print(true);
```

#### Clock
Returns the number of seconds since 0001-01-01:
```kotlin
print(clock());
```
