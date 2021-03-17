*Enforce Single Resource Extractor* enforces players only being able to use a single quarry and/or pump jack.
## Features


## Permissions


## Chat Commands


## Console Commands


## Configuration

- **Ignore Extractor Type** -- When set to `false` players will be able to run one Mining Quarry AND one Pump Jack at the same time

```json
{
  "Ignore Extractor Type": true
}
```

## Localization

The default messages are in the `EnforceSingleResourceExtractor.json` file under the `oxide/lang/en` directory. To add support for another language, create a new language folder (ex. `de` for German) if not already created, copy the default language file to the new folder, and then customize the messages.

```json
{
  "Warning Message Text": "<color=#FF7900>You can only run a single resource extractor at any given time.</color>"
}
```