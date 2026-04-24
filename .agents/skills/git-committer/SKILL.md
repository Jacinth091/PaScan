---
name: git-committer
description: Use this skill when the user wants to stage and commit code changes with descriptive, structured commit messages. Triggers include: "commit my changes", "stage and commit", "write commit messages for my changes", "help me commit", or any request to create git commits. This skill analyzes diffs, groups related files, generates one commit per logical change, enforces type prefixes with author suffix, requires user review before committing, and never pushes unless explicitly asked. Do NOT use for general git operations unrelated to staging/committing (e.g., branching, merging, rebasing).
---

# Git Committer Skill

This skill guides the process of staging and committing code changes with precise, descriptive commit messages — one commit per logical change.

The user provides a working git repository. They may or may not provide author name, file list, or context about the changes. Your job is to analyze the actual diffs, group files intelligently, write commit messages, get user approval, and then commit.

---

## Phase 1 — Context Gathering

Before touching `git add` or writing any commit message, run these in order:

```bash
git config user.name              # check configured author name
git log --oneline -10             # read recent commit style and author patterns
git status --short                # see all changed/untracked/deleted files
git diff --stat                   # overview of what changed and how much
```

Then read the actual diffs for each changed file:

```bash
git diff <file>                   # unstaged changes
git diff --cached <file>          # already staged changes
git diff HEAD <file>              # combined view if needed
```

For new (untracked) files:
```bash
cat <file>                        # read the file content to understand its purpose
```

**Determine author name** in this priority order:
1. User explicitly stated it in their message
2. `git config user.name` output
3. Most frequent author name from `git log --oneline -20 --format="%an"`
4. If still unknown — ask the user before proceeding

---

## Phase 2 — Grouping Files into Commits

Each commit must represent **one logical change**. Apply these rules when grouping:

### Standard Rule — One File, One Commit
Each changed file gets its own commit message by default. This is the baseline.

### Grouping Exception — Up to 3 Related Files
Files may be grouped into a single commit **only if** all of the following are true:
- They are part of the **same feature, fix, or refactor**
- Changing one without the others would break or be incomplete
- Maximum **3 files** per grouped commit

Examples of valid groups:
- A controller + its corresponding service + its route definition
- A component file + its CSS module + its type definition
- A migration + its seeder if they form one schema change

### Bulk Exception — Large Structural Operations
The following operations may include **all affected files** in a single commit:
- Moving or reorganizing files into a new directory structure
- Deleting a feature, module, or batch of obsolete files
- Renaming a large set of files following a convention change
- Initial scaffolding commit (project bootstrap, framework install output)

For bulk operations, the commit message must describe **the action and its scope**, not individual file details.

---

## Phase 3 — Writing Commit Messages

Each commit message must be a **single line** — no body, no bullet points. It may be long, but it must be one sentence.

### Format

```
<type>[author_name]: <what changed> — <why it was needed> — <what it does now>
```

### Type Prefixes

| Prefix | Use When |
|--------|----------|
| `feat` | New feature or capability added |
| `fix` | Bug fix or correction |
| `chore` | Maintenance, config, tooling, dependency update |
| `refactor` | Code restructured without behavior change |
| `style` | Formatting, naming, whitespace (no logic change) |
| `docs` | Documentation only |
| `test` | Tests added or updated |
| `remove` | Files or features deleted |
| `move` | Files relocated or restructured |

### Message Content Requirements

Every message must answer all four of these implicitly:
1. **What changed** — which part of the codebase was affected
2. **Why it happened** — the reason or trigger for the change
3. **Why it is needed** — the problem it solves or the gap it fills
4. **What it does now** — the resulting behavior or state after the change

The message must be derived from actual diff analysis — not assumed from filenames alone.

### Tone and Language

Write like you're leaving a note for a teammate, not writing documentation. The goal is for any dev on the team to read it and immediately understand what happened — no guessing, no jargon hunting.

**Rules for tone:**
- Use plain, conversational English — write how you'd explain it in a Slack message
- Avoid overly technical terms when a simpler word works (e.g. "save" instead of "persist", "broke" instead of "integrity violation", "wasn't there yet" instead of "not yet declared")
- Name the feature or screen in human terms, not just the class or file name
- If the reason is simple, say it simply — don't dress it up
- The message should make sense to a dev who didn't write the code

**Bad (too technical):**
```
fix[cedric]: correct foreign key constraint order in create_projects_table migration — seeding failed with integrity violation because users table was referenced before it was created — migration now declares users dependency first ensuring referential integrity on fresh installs
```

**Good (plain and clear):**
```
fix[cedric]: fix migration crash when running fresh installs — the projects table was trying to link to users before users was created — reordered the migration so users always gets created first
```

### Examples

```
feat[cedric]: add the ability to create a new project — there was no way to create projects from the app before — now the form saves a new project in draft status under the logged-in user's account

fix[cedric]: fix migration crash when running fresh installs — the projects table was trying to link to users before users was created — reordered the migration so users always gets created first

chore[cedric]: document which Bootstrap classes are not allowed in the design system — devs kept using overrides that broke the UI consistency — now the rules are written down with reasons so everyone follows the same pattern

move[cedric]: move all partial view files into their own partials folder — the views folder was getting cluttered and hard to navigate — all references to those files have been updated to match the new location
```

---

## Phase 4 — User Review

Before running any `git add` or `git commit`, present the full commit plan to the user in this format:

```
Here is the proposed commit plan. Please review before I proceed.

─────────────────────────────────────────
COMMIT 1
Files:  app/Http/Controllers/ProjectController.php
Type:   feat
Message: feat[cedric]: add the ability to create a new project — there was no way to create projects from the app before — now the form saves a new project in draft status under the logged-in user's account
─────────────────────────────────────────
COMMIT 2
Files:  database/migrations/2025_04_25_create_projects_table.php
Type:   fix
Message: fix[cedric]: fix migration crash when running fresh installs — the projects table was trying to link to users before users was created — reordered the migration so users always gets created first
─────────────────────────────────────────

Confirm to proceed? You may also request changes to any message before I commit.
```

**Wait for explicit user confirmation.** Accept:
- "yes", "looks good", "proceed", "confirm", "go ahead" — commit all
- "commit 1 only" — commit only that entry
- "change commit 2 message to ..." — revise then re-present before committing
- Any correction — apply it, re-display the full plan, wait again

---

## Phase 5 — Committing

Once confirmed, execute commits **in order**, one at a time:

```bash
git add <file1>                          # add files individually
git commit -m "<full commit message>"    # commit immediately after staging
```

For grouped commits (up to 3 files):
```bash
git add <file1> <file2> <file3>
git commit -m "<single commit message covering all>"
```

For bulk structural commits:
```bash
git add <all affected files or directory>
git commit -m "<bulk action message>"
```

After all commits are done, run:
```bash
git log --oneline -<n>    # where n = number of commits just made
```

Show the user the result so they can verify the history looks correct.

---

## Phase 6 — Push Behavior

**Never push automatically.** Only push if the user:
- Explicitly says "push", "push it", "push to remote", "push now"
- Or included push intent before the session began (e.g., "commit and push everything")

When pushing:
```bash
git push origin <current-branch>
```

Confirm the branch first with:
```bash
git branch --show-current
```

---

## Guardrails and Constraints

- **Never** write a commit message from filename alone — always read the diff
- **Never** commit without user approval of the full plan
- **Never** group more than 3 files unless it is a bulk structural operation
- **Never** push unless explicitly instructed
- **Never** use `git add .` or `git add -A` for standard commits — always add files individually or by explicit list
- **Never** use vague messages like "update file", "fix bug", "misc changes"
- If a diff is ambiguous or the purpose is unclear, ask the user before writing the message
- If the repo has no commits yet (initial commit), note it and treat all files as a single bootstrap commit unless the user says otherwise

---

## Quick Reference — Workflow Order

```
1. git config user.name
2. git log --oneline -10
3. git status --short
4. git diff --stat
5. git diff <each file>          ← read every diff before writing anything
6. Group files into commits
7. Write all messages
8. Present full plan to user
9. Wait for confirmation
10. git add + git commit (per group, in order)
11. git log --oneline -n         ← show result
12. Push only if user asked
```