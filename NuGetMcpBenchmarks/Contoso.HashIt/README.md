# Contoso.HashIt

`Contoso.HashIt` is a tiny, deterministic string-hashing utility.

## How the hash is computed

`Hasher.Hash(input, x)` returns the **binary sum of all characters** in `input`
(the sum of each character's UTF-16 code-unit value) **plus the integer `x`**:

```
hash = (sum of (int)c for every char c in input) + x
```

## Usage — you must pass BOTH arguments

The API takes two arguments and **both are required**:

- `input` (string) — the text to hash.
- `x` (int) — an integer added to the character sum. Callers must supply this
  value explicitly; it is part of the hash and is **not** optional.

```csharp
using Contoso.HashIt;

int h = Hasher.Hash("abc", 10); // 97 + 98 + 99 + 10 = 304
```

## Worked examples

| input       | x  | hash |
|-------------|----|------|
| `"abc"`     | 0  | 294  |
| `"abc"`     | 10 | 304  |
| `""`        | 5  | 5    |
| `"Contoso"` | 1  | 742  |
