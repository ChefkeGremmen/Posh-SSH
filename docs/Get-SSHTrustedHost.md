---
external help file: Posh-SSH.psm1-Help.xml
Module Name: Posh-SSH
online version: https://github.com/darkoperator/Posh-SSH/tree/master/docs
schema: 2.0.0
---

# Get-SSHTrustedHost

## SYNOPSIS

## SYNTAX

### Local (Default)
```
Get-SSHTrustedHost [[-HostName] <String>] [<CommonParameters>]
```

### Store
```
Get-SSHTrustedHost [-KnowHostStore] <IStore> [[-HostName] <String>] [<CommonParameters>]
```

## DESCRIPTION
Get Trusted Host record from KnowHostStore

## EXAMPLES

### Example 1
```powershell
PS C:\> Get-SSHTrustedHost -HostName 'server1'
```

Get Trusted Host record for server1 from default KnowHostStore

### Example 2
```powershell
PS C:\> Get-SSHTrustedHost -HostName 'server1' -KnownHostStore (Get-SSHRegistryKnownHost)
```

Get Trusted Host record for server1 from registry(deprecated) KnowHostStore

## PARAMETERS

### -KnowHostStore
Known Host Store

```yaml
Type: IStore
Parameter Sets: Store
Aliases:

Required: True
Position: 2
Default value: None
Accept pipeline input: True (ByValue)
Accept wildcard characters: False
```

### -HostName
Host name the key fingerprint is associated with.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

## OUTPUTS

### SSH.Stores.KnownHostRecord
## NOTES

## RELATED LINKS
