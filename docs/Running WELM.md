# Running WELM

WELM requires administrative rights to retrieve all event information. Specifically it needs administrative rights to get event information about the Security log, its related providers, and its events.

## Usage and examples
```
        Usage:
            welm.exe -h 
            welm.exe ([-p | -l | -e]) -f Format

        Options:
            -h --help                   Shows this dialog.
            -p --providers              Retrieve all providers.
            -l --logs                   Retrieve all logs.
            -e --events                 Retrieve all events.
            -f Format, --format Format  Specify format. txt, json, csv, or all.
```

Running **welm.exe -p -f all** retrieves all provider data in all supported formats.

Running **welm.exe -l -f all** retrieves all log data in all supported formats.

Running **welm.exe -e -f all** retrieves all event data in all supported formats.

Instead of using **all**, you can retrieve data in specific formats:

* **-f txt**
* **-f json**
* **-f csv**

The provided [welm.bat](../welm/welm.bat) file runs WELM so that event information is retrieved in all formats.

## Output

Running WELM results in the following files being generated:

* **classicevents.txt**/**.json**/**.csv** - Contains metadata for classic style events. Available for Windows XP and later.
* **classiclogs.txt**/**.json**/**.csv** - Contains metadata for classic style logs. Available for Windows XP and later.
* **classicsources.txt**/**.json**/**.csv** - Contains metadata for classic style event sources. Available for Windows XP and later.
* **events.txt**/**.json**/**.csv** - Contains metadata for new style events. Available for Windows Vista and later.
* **logs.txt**/**.json**/**.csv** - Contains metadata for new style logs. Available for Windows Vista and later.
* **providers.txt**/**.json**/**.csv** - Contains metadata for new style providers. Available for Windows Vista and later.
* **welm**.***yyyyMMddHHmms***\_***loglevel***.**txt** - Log files useful for observing WELM's internal operations.

**"Classic style" versus "new style"**. The "new style" metadata is retrieved using Windows event log APIs introduced in Windows Vista with the new event log system often referred to by its codename of Crimson. The "classic style" metadata is retrieved from the Windows registry for log metadata and source metadata and from text resources embedded in binaries for event metadata.

Some of the classic style events are just normal strings (UI text elements, etc) that are embedded in the binary. Unfortunately there isn't a reliable way to differentiate between an event text string and a normal text string (that's used for other uses) that doesn't result in losing legitimate event text strings. The new style event data does not have this problem.