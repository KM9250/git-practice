# CLAUDE.md

This file provides guidance for AI assistants working in this repository.

## Repository Overview

This is a minimal **git practice repository** (`KM9250/git-practice`). It currently serves as a sandbox for learning and experimenting with Git workflows.

## Current State

The repository is in an early/bare state with only two files:

| File | Description |
|------|-------------|
| `README.md` | Contains placeholder text (`hogehoge`) |
| `.gitignore` | Empty file, no ignore rules defined |

There is one commit in the history:
```
fde0b4b created .gitignore README.md
```

## Repository Structure

```
git-practice/
├── .git/               # Git metadata (do not modify directly)
├── .gitignore          # Currently empty
├── README.md           # Placeholder content
└── CLAUDE.md           # This file
```

## Branch Conventions

- `master` — default/main branch
- `claude/<description>-<session-id>` — branches used by AI assistants for scoped changes

Active branches:
- `master`
- `claude/add-claude-documentation-AhjUq` (current)

## Development Workflow

Since this is a practice repo with no build system, test suite, or dependencies, the workflow is pure Git:

1. **Create a branch** for your changes off `master`
2. **Make changes** to files
3. **Commit** with a clear, descriptive message
4. **Push** with `git push -u origin <branch-name>`
5. **Open a PR** when the work is ready for review

## Git Practices

- Commit messages should be concise and describe *what* changed and *why*
- Branch names follow the pattern: `claude/<feature-description>-<session-id>` for AI-driven work
- Never force-push to `master`
- The `.gitignore` is currently empty — add entries here when the repo grows to include build artifacts, editor files, secrets, etc.

## No Build / Test System

This repository has **no build system, test runner, linter, or package manager** configured. There are no commands to run for setup, testing, or building.

If these are added in the future, document them here:

```bash
# Placeholder — update when applicable
# npm install     # install dependencies
# npm test        # run tests
# npm run build   # build project
```

## Notes for AI Assistants

- This repo is intentionally minimal; avoid over-engineering changes
- Update this `CLAUDE.md` whenever significant new files, tooling, or conventions are introduced
- Keep the `README.md` updated with human-readable project context when the project purpose becomes clearer
