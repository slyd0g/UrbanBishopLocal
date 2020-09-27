# UrbanBishopLocal

![UrbanBishopLocal](https://raw.githubusercontent.com/slyd0g/UrbanBishopLocal/master/example.png)

## Description
A port of [FuzzySecurity's](https://twitter.com/FuzzySec) [UrbanBishop](https://github.com/FuzzySecurity/Sharp-Suite#urbanbishop) project for inline shellcode execution.

- ```NtCreateSection``` is used to create a section object
- ```NtMapViewOfSection``` creates a section view with RW permissions we can write shellcode to
- Shellcode is written to the section view
- A second call to ```NtMapViewOfSection``` creates a section view with RX permissions
- A pointer to the base address of the shellcode is converted to a delegate and executed

## Usage
1. Base64 encode XOR encrypted 64 bit shellcode with PowerShell
    - ```[Convert]::ToBase64String([System.IO.File]::ReadAllBytes("$PSScriptRoot\encrypted_shellcode.bin")) | clip```
2. Copy base64 string into ```Program.cs```
3. Replace your XOR key within ```Program.cs```
4. Build the project for x64

