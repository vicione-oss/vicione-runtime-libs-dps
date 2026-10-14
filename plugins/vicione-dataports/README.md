# vicione-dataports

Claude Code plugin carrying the conventions every ViciOne DataPort provider shares — what the
built tree guarantees, how an unreadable value is handled, what may travel on the wire, where
validation belongs, and what a provider may depend on — and how a provider is built on the
`DataPort.Extensions` base classes.

It ships one skill, `dataport-conventions`, which Claude reads on its own when a task builds or
extends a provider, or touches a provider's wire format, its handling of received messages, or its
validation.

The rules are extracted from provider reviews. Changing one changes it for every provider at once,
which is the point — the alternative was the same review comment in six repositories.

## Use it in a provider repository

Add `.claude/settings.json` to the provider repository and commit it, so everyone working there
gets the plugin once they trust the folder:

```json
{
  "extraKnownMarketplaces": {
    "vicione": {
      "source": {
        "source": "url",
        "url": "https://gitlab.com/vicione-oss/vicione/runtime-libs/dps.git",
        "ref": "main"
      }
    }
  },
  "enabledPlugins": { "vicione-dataports@vicione": true }
}
```

Claude Code uses the git credentials already on the machine, so the private repository needs no
further setup.

## Change it

The skill is `skills/dataport-conventions/SKILL.md`, with the build guide's detail in its
`references/`. Raise `version` in
`.claude-plugin/plugin.json` **and** in the marketplace entry in `dps/.claude-plugin/marketplace.json`
— a provider repository only picks up a change when that number moves.

Before pushing:

```sh
claude plugin validate ./plugins/vicione-dataports
claude --plugin-dir ./plugins/vicione-dataports
```
