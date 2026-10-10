## Plan: Two Sum (C#)

TL;DR: Implement a single-pass hash-map `TwoSum` method in C# (O(n) time, O(n) space), add a minimal console runner and a unit test, and place the implementation in the CSharpDev folder for easy editing and verification.

**Steps**
1. Create `TwoSum` method (public static) that takes `int[] nums` and `int target` and returns `int[]` of two indices. *Depends on step 2 for file placement.*
2. Add a small console runner that demonstrates the method with at least two test cases. *Can be done in parallel with step 3.*
3. Add a unit test file using a simple test harness (NUnit/xUnit) or a minimal assertion-based test runner. *Depends on test tooling availability.*
4. Verify by compiling and running the console runner; run unit tests if a test project is created.
5. Optionally, refactor into library + tests if you want later.

**Relevant files**
- CSharpDev/test1.cs — current file; either modify this file or create new files here
- CSharpDev/TwoSum.cs — proposed implementation file (create)
- CSharpDev/TwoSumTests.cs — proposed simple test harness (create)

**Verification**
1. Compile and run with `dotnet` or `csc`: ensures code compiles and console runner prints expected indices.
2. Run unit tests (if using `dotnet test`) or execute the test harness to assert correctness on multiple cases including negative numbers, duplicates, and minimal arrays.

**Decisions**
- Algorithm: single-pass dictionary (hash map) returning the first matching pair. Handles negative numbers and duplicates correctly by storing indices.
- Language: C# (confirmed).

**Further Considerations**
1. If you prefer a specific test framework, tell me (NUnit, xUnit, MSTest). Otherwise I'll use a minimal self-contained runner to avoid requiring project scaffolding.
2. If you want the implementation inserted directly into `CSharpDev/test1.cs`, confirm and I'll produce the code and tests next.
