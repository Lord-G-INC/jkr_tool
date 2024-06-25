# JKR_Tool
A tool for packing and unpacking Nintendo's JKR RARC or CRAR archives.
## Usage
```
jkr_tool <FileOrDir1> [FileOrDir2] [FileOrDir3] [...]
```
Will convert all given files/folders.

When given a folder, it will be packed into a RARC archive.

When given a file, it will be unpacked into a folder.

*Note:* The endian is determined by the RARC/CRAR archive's file magic.
