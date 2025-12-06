# Git Commit Message Conventions

This project follows Conventional Commits format for all commit messages.

## Format

```
<type>(<scope>): <subject>

[optional body]

[optional footer]
```

## Types

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, no logic change)
- **refactor**: Code refactoring
- **perf**: Performance improvements
- **test**: Adding/updating tests
- **build**: Build system or external dependencies
- **ci**: CI/CD pipeline changes
- **chore**: Maintenance tasks, tooling, configurations
- **revert**: Revert a previous commit

## Rules

### Subject Line
- **Use imperative mood**: "Add feature" not "Added feature"
- **Keep concise**: Under 50 characters, no period at the end
- **Include scope** only when clearly identifiable (e.g., `api`, `ui`, `auth`, `docs`, `cli`, `core`, `data`)
- **Be descriptive**: Explain intent and purpose, not file names or implementation details
- **Avoid generic messages**: No "update files" or "add configs"—be specific and purposeful

### Body (Optional)
- **Wrap at 72 characters** per line
- **Explain what and why**, not how
- **Use bullet points (*)** for clarity when listing details
- **Focus on purpose**: Describe the value and effect of the change

### Footer (Optional)
- Reference issues: `Closes #123`, `Fixes #456`
- Breaking changes: `BREAKING CHANGE: description`

## Tone and Style

- **Clear, concise, and value-oriented**
- **Focus on purpose and effect**, not files modified
- **Each commit tells a story**: One logical change with clear reasoning
- **Answer "why?"** not just "what?"

## Examples

### ✅ Good Commits

```
feat(rag): implement vector search for document analysis

Uses PostgreSQL pgvector to retrieve top-K relevant chunks
before sending to AI, reducing API costs by 70%.

Closes #42
```

```
refactor(core): consolidate chunking quality metrics

* Moved quality calculation into ChunkQualityAnalyzer
* Normalized scores to 0-1 range for consistency
* Added token count validation
```

```
chore(deps): upgrade to .NET 10 and C# 14
```

```
docs(readme): update prerequisites for VS 2026
```

```
fix(cli): handle null embeddings in batch operations

Prevents NullReferenceException when API returns empty
results during high-traffic periods.
```

### ❌ Bad Commits

```
update files
```
*Too generic, no context*

```
Added new feature to Program.cs
```
*Past tense, mentions file name, unclear purpose*

```
fix: updated the configuration settings in appsettings.json file
```
*Mentions file name, no explanation of what or why*

```
refactor: code improvements
```
*Vague, no specific information*

## Quick Reference

**Format Template:**
```
<type>(<scope>): <imperative verb> <what>

Why this change was needed and what it accomplishes.

* Detail 1
* Detail 2

Closes #issue
```

**Common Scopes:**
- `cli` - Command-line interface
- `core` - Core domain logic
- `ai` - AI/LLM services
- `data` - Database/persistence layer
- `rag` - RAG architecture components
- `docs` - Documentation
- `deps` - Dependencies
- `config` - Configuration files

---

**Remember**: A good commit message explains the "why" behind the change, making future maintenance and code archaeology easier.
