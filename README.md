# Auto Login Wifi

## Command Info

```bash
AutoWifiLogin 1.1.1
Copyright (C) 2021 Weiting

  -u, --username    Required. Your account

  -p, --password    Required. Your password

  -t, --Type        Runtime Machine [ Mac, Win ], Default: Mac

  --help            Display this help screen.

  --version         Display version information.
```

## How to use

### dotnet

```bash
dotnet tool install --global AutoWifiLogin

autowifilogin -u username -p password -t Win
```

### docker

```bash
docker run --rm ghcr.io/weiting-tw/autowifilogin/autowifilogin:latest -u username -p password -t Win
```
