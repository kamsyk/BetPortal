---
name: branch-revise
description: Standard revision pass on the current git branch before it's PR-ready — rebase onto master, verify build/tests, run a code review + simplification pass, and clean up commit history. Use when the user asks to "revise", "clean up", or "finalize" the current branch.
---

Run these steps in order. Stop and ask the user before any step that rewrites shared history or force-pushes.

1. **Establish context**
   - `git status` — if the working tree isn't clean, stash or ask the user before continuing.
   - `git fetch origin master`
   - `git log origin/master..HEAD --oneline` to see what's on this branch.

2. **Rebase onto latest master**
   - `git rebase origin/master`
   - If the branch may already be pushed/shared, tell the user this will rewrite history and confirm before rebasing.
   - On conflicts: resolve with the user's input rather than guessing which side wins. Never `git rebase --abort` silently — explain why first.

3. **Build & test verification**
   - `dotnet build BetPortal.sln`
   - If any test projects exist (glob for `**/*Test*.csproj` / `**/*Tests*.csproj`), run `dotnet test` against them.
   - Fix any errors before moving on — don't proceed with a broken build.

4. **Code review pass**
   - Invoke the `code-review` skill against the diff (`origin/master...HEAD`) to catch correctness bugs.
   - Invoke the `simplify` skill on the same diff for reuse/simplification/efficiency cleanup.
   - Apply the fixes, then re-run step 3 to confirm the build/tests still pass.

5. **Commit history cleanup**
   - Review `git log origin/master..HEAD` for WIP/fixup commits, unclear messages, or commits that should be squashed/reordered.
   - Propose a cleanup plan (squash/reword/reorder) to the user and get confirmation before rewriting.
   - Perform the rewrite only after confirmation. Never force-push as part of this skill — that decision is the user's.

6. **Final summary**
   - `git status` and `git log origin/master..HEAD --oneline` to show the resulting state.
   - Summarize what changed: commits rewritten, bugs fixed, simplifications applied.
