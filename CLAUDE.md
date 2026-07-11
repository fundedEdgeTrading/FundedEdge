# CLAUDE.md

# Mission

Implement the requested change using the minimum possible context and the minimum possible token usage while preserving correctness.

Token efficiency is a first-class objective.

---

# Repository Exploration

Only inspect files that are directly required.

Never scan the entire repository.

Never enumerate directories unless necessary.

Stop searching as soon as enough context exists.

Prefer symbol search over reading files.

Prefer targeted searches over recursive exploration.

Never reopen files already inspected unless they changed.

Avoid reading generated code.

Avoid reading lock files.

Avoid reading build artifacts.

Avoid reading vendor or third-party code.

Avoid reading documentation unless explicitly requested.

---

# Code Changes

Modify the smallest possible amount of code.

Prefer extending existing implementations.

Do not refactor unrelated code.

Do not rename files unless necessary.

Do not reformat unrelated files.

Preserve existing architecture.

Preserve existing coding style.

Avoid introducing new abstractions unless they clearly simplify the solution.

---

# Output

Keep responses extremely concise.

Default response:

- files changed
- one sentence describing the implementation
- build/test result

Do not explain implementation.

Do not explain why the solution works.

Do not summarize code.

Do not include code snippets unless requested.

Never repeat the user's request.

---

# Git

When implementation is complete:

- stage modified files
- create one meaningful commit
- push current branch

Never:

- create Pull Requests
- generate PR descriptions
- generate PR titles
- inspect GitHub metadata
- compare branches
- fetch unnecessary remote information

If there are no code changes:

do not commit.

do not push.

Only one commit per task.

---

# Testing

Only run what is necessary.

Prefer:

- affected project build
- affected unit tests

Never execute:

- complete solution build
- integration tests
- end-to-end tests
- benchmarks

unless explicitly requested.

If confidence is already high, avoid redundant executions.

---

# Search

Prefer:

rg
grep
symbol search

Avoid opening large files.

Read only relevant sections.

Stop searching immediately once enough information exists.

---

# Context

Keep as little context as possible.

Forget unrelated files after finishing a task.

Do not retain unnecessary repository knowledge.

Avoid discussing unrelated modules.

---

# Communication

Never produce long explanations.

Never produce tutorials.

Never produce architectural discussions unless requested.

Avoid markdown tables.

Avoid long bullet lists.

Answer using fewer than 150 words whenever possible.

---

# Decision Making

When multiple valid solutions exist:

Prefer:

- fewer files
- fewer modified lines
- lower complexity
- existing utilities
- existing dependencies

Avoid introducing:

- new packages
- new frameworks
- new services
- unnecessary abstractions

---

# Performance

Avoid unnecessary commands.

Avoid repeated searches.

Avoid repeated builds.

Avoid repeated file reads.

Avoid repeated git commands.

Do not verify something twice.

Trust previous successful results.

---

# Default Behaviour

Think.

Locate.

Modify.

Build only if needed.

Commit.

Push.

Stop.

Assume existing code is correct unless the requested task requires investigating it.

Do not inspect surrounding modules out of curiosity.

Never read more than one level of dependencies.

If File A references File B, inspect B only if strictly required.

Do not recursively explore dependency chains.

Every file opened and every command executed must have a clear purpose.

If a file or command does not directly contribute to solving the user's request, do not read or execute it.

# For .NET repositories

Prefer:

- solution-wide search only when symbol lookup fails
- build the affected project instead of the entire solution
- inspect only the project containing the modified code

Avoid:

- rebuilding every project
- restoring packages if already restored
- reading csproj files unless dependencies change
- reading launchSettings.json
- reading appsettings unless configuration is involved