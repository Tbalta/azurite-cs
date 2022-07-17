# azurite-cs
Csharp edition of the azurite language.\
Azurite is a lisp like pattern matching programming language.

## Installation
Clone the repository and build the project.
You can also clone the standard library from: https://github.com/Azurite-Language/azurite-stdlib

## Usage
basic usage:
```lisp
(macro "//" ())
// Without this line comment will not be recognize.

(import "stdlib/stdmath/std_math")
(// Import all translates defineds in std_math)

(std_math azur)
(// Intialize the translates in the language azur)
(+ 2 5)
```

## Translates
This is the core of the language, translates allow us to defines the pattern matching and the behavior of the language. For instance let’s define a translate for the function `+`:
```lisp
(translate ("+" a b) ("any" "any" "any") (azur "" "{a} + {b}"))

```
The translate defined here will be used to match any expression of the form `(+ a b)` with a and b being any expression.

There is different ways to specify the "level" of matching of the parameters of the translate.
### The strong matching
When the name of the paramters is between double quote, like `"+"` it will match the token +.

### The optional strong matching
```lisp
(translate ("foo|x|bar") ("any") (azur "" "{x}"))
```
When the name of the parameters is between pipes, it can match anything as long as the element at the left and at the right are the same. For example the following lines will be matched:
```lisp
(foofoobar) (// x: foo)
(foobar) (// x: )
```
The following lines will not be matched.
```lisp
(foofoba) 
```

### The weak matching
```lisp
(translate ('+' a b) ("any" "any" "any") (azur "" "{a} + {b}"))
(// Please do not the ')
```
When the parameter is between single quote, like `'+'` it will be matched with any atom expression. The following lines will be matched:
```lisp
(+ 2 3) (// +: +, a: 2, b: 3)
(- 2 3) (// +: -, a: 2, b: 3)
(+ 2 (- 4 5)) (// +: +, a: 2, b: (-4 5))
```
The following lines will not be matched:
```lisp
((+ 2 5) 5 6) (// the first symbol is not an atom)
(+ 2 5 5) (// There is too much arguments)
(+ 2) (// There is not enough arguments)
```

### The weak matching
```lisp
(translate (+ a b) ("any" "any" "any") (azur "" "{a} + {b}"))
```
When the parameter is left alone, like `+` it will be matched with any expression. The following lines will be matched:
```lisp
((+ 2 3) 2 3) (// +: (+ 2 3), a: 2, b: 3)
(+ 2 3) (// +: +, a: 2, b: 3)
(foo 2 3) (// +: foo, a: 2, b: 3)
```
The following lines will not be matched:
```lisp
(+ 2 3 5) (// There is too much arguments)
(+ 2) (// There is not enough arguments)
```
