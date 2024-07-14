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
This is the core of the language, translates allow us to defines the pattern matching and the behavior of the language. For instance let's define a translate for the function `+`:
```lisp
(translate ("+" a b) ("any" "any" "any") (azur "" "{a} + {b}"))
```

The translate defined here will be used to match any expression of the form `(+ a b)` with a and b being any expression.

**Exercice - 1**
- Create a translate for the function `-` and `*`, transforming the expression `(- 5 (* 2 3))` into `(5 - (2 * 3))`.


### The strong matching
When the name of the paramters is between double quote, like `"+"` it will match the token +.

**Exercice - 2**
- Write two translates for the function `*`:
    - The first one will assume you have a double function, and will transform `(* 2 3)` into `double(3)`
    - The second one will transform `(* 1 3)` into `(1 * 3)`.
*Tips*: The precedence of one translate is the order of appearance of the translate in the code, the first translate will have the highest precedence.

### The weak matching
When the name of the parameters is between single quote, like `'+'` it will match with any atom expression.

**Exercice - 3**
- Write 2 translates for transforming a variable declaration.
    - The first translate will transform `(let a 5)` into `a = 5`
    - The second one will be used to have an error in case of a wrong variable name, transforming `(let (+ 5 5) 5)` into `(error)`
*Tips*: Usually no error translate are created, but it can be useful in some cases.

### The optional strong matching
When the name of the parameters is between pipes, it can match anything as long as the element at the left and at the right are the same, like `foo|a|foo`. Will match `foo bar foo` but not `foo bar baz`.


**Exercice - 4**
- Write 11 translates to detect if a number is even or odd
    - Transform `(is_even 2)` into `2 is even`
    - Transform `(is_even 3)` into `3 is odd`
    - Transform `(is_even 42)` into `42 is even`
    - Transform `(is_even 55)` into `55 is odd`


### List
__Embrace parenthesis__

A list is a sequence of elements separated by space and embraced by parenthesis. For instance `(1 2 3)` is a list of 3 elements `1`, `2` and `3`. `(+ 1 2)` is also a list of 3 elements `+`, `1` and `2`, whereas `((+ 1 2))` is a list of 1 element `(+ 1 2)`.

When the name of the parameters is suffixed by `...`, it's mean that parameter will be considered as a list, the parameter will match anything as long it's not an atom. Special rules are applied when replacing the parameter in the body.

```lisp
(translate ("list" param...) ("any" "any") (azur "" "{param (param separator head tail)}"))
```
The head and tail fields are optional, but if one is present, the other must be present too.

**Exercice - 5**
- Write a translate for the function `list` that will transform `(list (1 2 3))` into `[1,2,3]`.


__Going crazy__
_Have you noticed the format of the body is exactly the same as an expression?_
Well, it's because it is an expression. `param`, `separator`, `head` and `tail` are expressions that will be evaluated before being replaced in the body.
_Note_: The original expression is used when evaluating the parameters, no transformations are applied before.

**Exercice - 6**
- Write a translate for the function `list` that will transform `(list (1 2 3))` into `[1 * 3, double(3), 3 * 3]`.

### The eval balise
__Even crazier__
Expressions placed between `<eval >` will be re-evaluated once all the parameters are replaced in the body.

```lisp
(translate ("=" "|x|2" "|y|2") ("any" "any") (azur "" "<eval (= {x} {y})>"))
(translate ("=" "1" "1") ("any") (azur "" "true"))
(= 1 1)
(= 12 12)
```
The code above will output:
```
true
true
```

**Exercice - 7**
- Let's create a translate for the `if` function that will transform `(if (= 1 1) 2 3)` into `2` and `(if (= 1 2) 2 3)` into `3`.
    - Start by extending the previous example to support the comparison to all numbers.
    - Then create a translate for the `if` function.




# Macro
Macro are evaluated from the more nested to the less nested.
```lisp
(second (first))
```
## macro matching
Two factor are used for the macro matching.
* The form of the AST
* Matching level of different node in the AST

### The form of the AST
```lisp
(macro ("inverse" a b) (b a))
```
this macro will match the expression `(inverse a b)` with a and b being any expression.
```lisp
(macro ("inverse2" (a) b) (b a))
```
this macro will match the expression `(inverse2 (a) b)` with a and b being any expression.
For instance:
```lisp
(inverse2 (1) 4) (// inverse2: 4 1)
(inverse2 ((1 2 3)) 4) (// inverse2: 4 (1 2 3))
(inverse2 ((1 2 3)) (1 2)) (// inverse2: (1 2) (1 2 3))
```