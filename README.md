# Distributary
A module aimed at simplifying the management of output streams, logs, and more... To be consumed by other modules, scripts, etc...
[Distributary - WikiPedia](https://en.wikipedia.org/wiki/Distributary) - *A distributary, or a distributary channel, is a stream that branches off and flows away from a main stream channel.*

```PowerShell

>> Write-OutStream "Some Output Text" -OutStream Debug

DEBUG: [2022-10-21 13:01:38] Success:   [Unknown]       :       Some Output Text
```

```PowerShell
>> Write-OutStream "Some Output Text" -OutStream Verbose

VERBOSE: [2022-10-21 13:01:38] Success:   [Unknown]       :       Some Output Text

```

```PowerShell
>> Write-OutStream "Some Output Text" -OutStream Verbose

VERBOSE: [2022-10-21 13:01:38] Success:   [Unknown]       :       Some Output Text

```
