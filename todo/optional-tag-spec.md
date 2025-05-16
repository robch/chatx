**Title:**  Add “optional” Test-Case Support (Off-by-Default Tests)

---

## Problem Statement

Right now all tests without a `--remove TAG` are run by default, and users must explicitly include or exclude by tags. We need a way for test authors to mark certain tests (or entire areas) as off-by-default, so that casual runs don’t execute them, but power users can opt in when desired.

---

## Proposed Feature: `optional` Key

1. **Top-Level Key**

   * Add a new (optional) test-case and area attribute:

     ```yaml
     optional: <string> | [<string>, …]
     ```
   * Semantics: any test or area with an `optional` key is *excluded* by default.

2. **Inheritance & Additivity**

   * If an `area:` or `class:` block includes `optional: X`, all descendant tests inherit that value.
   * Child nodes may specify their own `optional:` (string or array) to *add* categories.

3. **CLI Behavior**

   * **Default** (`cycodt run`):

     * Runs only tests **without** any `optional` key.
   * **Re-include All Optionals** (`cycodt run --include-optional`):

     * Includes *all* tests marked `optional`, regardless of their category.
   * **Re-include by Category** (`cycodt run --include-optional slow network needsAI`):

     * Only re-includes tests whose `optional` array intersects the provided list.
   * Compatible with existing flags (`--contains`, `--remove`)—all filters stack in order.

4. **Tag vs. Optional**

   * **Tags** remain purely user-controlled filters—no change in default inclusion logic.
   * **Optional** is the sole mechanism for off-by-default semantics; tags continue to drive inclusion/exclusion as today.

5. **Logging & Debug**

   * No visible “X tests skipped” summary in normal stdout.
   * Internally log skipped and re-included counts under debug log level.

---

## Examples

### 1) Single Test Opt-in

```yaml
- name: Slow integration test
  command: ./run-heavy.sh
  optional: slow
```

* `cycodt run` ☑️ skips
* `cycodt run --include-optional` ☑️ runs
* `cycodt run --include-optional slow` ☑️ runs
* `cycodt run --include-optional network` ❌ still skips

---

### 2) Area-Wide Opt-in

```yaml
- area: Flaky network checks
  optional: [network,flaky]
  tests:
    - name: Unstable endpoint
      command: curl http://unstable.svc/
```

* Descendant tests inherit both categories
* `cycodt run --include-optional flaky` will include these

---

### 3) Combined with Tag Filters

```bash
# Run everything except optional tests tagged "slow":
cycodt run --remove slow
# Run only area tests (including optional) tagged "network":
cycodt run --include-optional --contains network
```

---

## Acceptance Criteria

* [ ] Parser recognizes `optional:` at the test, area, and class levels (string or list).
* [ ] Default run excludes any test with an `optional` key.
* [ ] `--include-optional [<categories>…]` toggles re-inclusion correctly.
* [ ] Optional inheritance: parent → child additive behavior.
* [ ] Existing tag filters (`--contains`, `--remove`) continue to work and stack.
* [ ] Debug-level logging shows skip/re-include counts; no stdout noise.

---

Let me know if you’d like to adjust any part of the user-facing behavior or add more example scenarios!
