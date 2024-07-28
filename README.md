# azurite-cs
Csharp edition of the azurite language.\
Azurite is a lisp like pattern matching programming language.

## Installation
Clone the repository and build the project.
You can also clone the standard library from: https://github.com/Azurite-Language/azurite-stdlib

## Vocabulary
* **S-expression**:
    * An atom such as `x`, `5` or `"hello"`
    * An expression under the form (a b ... n), where a, b and n are also S-expressions
* **Parameter**:
    * A placeholder for an S-expression that will be used in the body of a translate


## Translates
This is the core of the language, translates allow us to defines the pattern matching and the behavior of the language. By associating S-expression to a parameter name.

For instance let's define a translate for the `(+ a b)` S-expression:
```lisp
(translate ("+" a b) ("any" "any" "any") (azur "" "{a} + {b}"))
```

The translate defined here will be used to match any S-expression of the form `(+ a b)` with a and b being any S-expression.

**Exercice - 1**
- Create a translate for the function `-` and `*`, transforming the S-expression `(- 5 (* 2 3))` into `(5 - (2 * 3))`.


### The strong matching
When the name of the paramters is between double quote, like `"+"` it will only match the atom  `+`.

**Exercice - 2**
- Write two translates for the function `*`:
    - The first one will assume you have a double function, and will transform `(* 2 a)` into `double(a)`
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
When the name of the parameters is between pipes, it match atoms with the same left and right elements.
```lisp
(translate ("left|a|right") ("any") (azur "" "{a}"))

(translate ("multiple_of_ten" "|x|0") ("any") (azur "" "true"))
(translate ("multiple_of_ten" 'a') ("any") (azur "" "false"))

(translate ("negative" "-|x|") ("any") (azur "" "true"))
(translate ("negative" 'x') ("any") (azur "" "false"))


(left_foo_right)
(multiple_of_ten 250)
(multiple_of_ten 11)
(negative -5)
(negative 5)
```
The code above will output:
```
_foo_
true
false
true
false
```

`left|a|right`. Will match `left_foo_right`, with a being `_foo_` but not with `left_bar_baz`.


The left and right values are optional, if the 2 are omitted, the translate will act the same as a weak match.

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
In this body: `{param (param separator head tail)}`,
* `{param ...}` indicate that this section is related to the parameter `param`.
* `(param separator head tail)` is a list of 4 elements
    * `param`, for the moment this should be the same as the original parameter
    * `separator` is the separator between the elements
    * `head` this element open the list
    * `tail` this element enclose the list
**Note**:The head and tail fields are optional, but if one is present, the other must be present too.

*Example*:
```lisp
(translate ("return_add" param...) ("any" "any") (azur "" "{param (param + return ;)}"))
```
This translate will transform `(return_add (1 2 3))` into `return 1 + 2 + 3;`.


**Exercice - 5**
- Write a translate for the function `list` that will transform `(list (1 2 3))` into `[1,2,3]`.


__Going crazy__\
_Have you noticed the format of the body is exactly the same as an expression?_\
Well, it's because it is an expression. `param`, `separator`, `head` and `tail` are expressions that will be evaluated before being replaced in the body.
_Note_: The original expression is used when evaluating the parameters, no transformations are applied before.

**Exercice - 6**
- Write a translate for the function `list` that will transform `(list (1 2 3))` into `[1 * 3, double(3), 3 * 3]`.

### The eval balise
__Even crazier__\
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
    - Start by extending the previous example to support the comparison between all numbers.
    - Then create a translate for the `if` function.

In our current implementation, we are not checking beforehand if the "=" can be evaluated. (= 1 x) will return "false" no matter the value of x.

**Exercice - 8**
- Let's write the ("can_eval" "=" 'a' 'b') translate that will return true if the "=" can be evaluated
    - The translate should return true if a and b are numbers
    - For all other cases, it should return false
- Write the ("try_eval" "=" "\<cond\>" 'a' 'b')  translate that will call (eval = a b) if cond is true, other wise return (a = b)
- Write the ("=" a b) translate that will call try_eval and set the cond accordingly
*Tips*:
- The util.azur contains `(is_number a)`, `(and a b)` import it with `(import "./filepath/util")`
- The eval balises can be nested as in `<eval (test <eval (...)>)>`


In this current setup we have a problem evaluating `(= 2 (+ 1 1))` if we define (+ a b) as `a + b`, if you try it, the following error will be raised:
```
501 Unable to evaluate the expression inside the eval: <eval (can_eval = 2 (1 + 1))> at line 5: (can_eval = 2 (1 + 1))
501 No translate found in azur at line -1: (can_eval = 2 (1 + 1))
```

To counter this problem, instead of refering the argument as {a} and {b}, we will put it between square brackets, like this: [a] [b], in a context of an eval this signify the a and b parameters must be replaced as is, without any transformation.

```
(translate ("=" a b) ("any" "any" "any") (azur "" "<eval (try_eval = (can_eval = [a] [b]) [a] [b])>"))
```

## Macro
Macro are used to transform the S-expression before translates. From our previous example, we might want to extands the (can_eval "=" a b) translate.

Man can be tempted to write the following translates:
```lisp
(translate ("can_eval" "=" a b) ("any" "any" "any") (azur "" "<eval (and (can_eval [a]) (can_eval [b]))>"))
(translate ("can_eval" 'a') ("any" "any") (azur "" "<eval (is_number {a})>"))
```

However when we will try with (can_eval = (+ 1 1) 2), the following error will be raised:

```
501 No translate found in azur at line -1: (can_eval (+ 1 1))
```

The solution is to create macro transforming (can_eval (+ 1 1)) into (can_eval + 1 1).

```lisp
(macro ("can_eval" ("+" a b)) (can_eval + a b))
```
