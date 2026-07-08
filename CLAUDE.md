# CLAUDE.md

## Primary Goal
Your primary objective is to minimize token usage while maintaining correctness.
Assume this repository is large. Reading unnecessary files is considered a failure.

## Core Rules
- Never scan the entire repository.
- Only inspect the files needed for the current task.
- Do not reopen files already in context.
- Make the minimum code changes required.
- Avoid unrelated refactors, formatting-only edits, or renames.
- Do not explain code unless requested.

## Search Strategy
1. Think.
2. Identify the most likely file.
3. Read one file.
4. If insufficient, read one more.
5. Stop searching as soon as enough information is available.

Never recursively inspect folders or perform repeated repository-wide searches.

## Large Repository Policy
Avoid reading every project, README, solution or folder.
Locate only what is necessary.

## Editing Policy
- Modify only directly involved files.
- Preserve existing style and architecture.
- Never move or rename files unless requested.

## Git Policy
- Do not inspect history, blame or commits unless requested.

## Build Policy
Prefer:
- dotnet build <project>
instead of:
- dotnet build solution.sln

Only build affected projects.

## Testing Policy
Run only affected tests.

## .NET
- Respect .editorconfig, nullable settings and analyzers.
- Do not change formatting outside edited code.

## Token Optimization
Every file read has a cost.
Every search has a cost.
Prefer reasoning over exploration.
Avoid repeated searches for already found symbols.

## Incremental Work
Implement exactly what was requested.
Do not anticipate future requirements.

## Communication
Default verbosity: LOW.
Default explanation: MINIMAL.
Return only what is necessary unless more detail is requested.

## Repository Assumptions
Assume the project builds and conventions are intentional unless told otherwise.

## Performance Priority
1. Correctness
2. Low token usage
3. Minimal file reads
4. Fast completion
5. Code elegance
