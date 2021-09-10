# Archiver
**Cross platform, multi-destination backup and archiving utility**

## Overview
This tool is designed to allow for data to easily be cataloged, backed up, 
and optionally "cold storage" archived using a variety of data backup 
destinations. The tool is also intended to have cross platform support.

## Features
* Cross Platform (Linux & Windows)
* Backup to tape drives
* Backup to optical discs (CD, DVD, and BluRay)
* Backup to cold-storage storage drives (HDD, SSD, SD, Flash Drive, etc)
* Verify integrity of backup media
* Catalog and search all files that are written to archive media

## Goals
My overall goals for this application (in no particular order) are as follows:

* Cross-platform support (Windows + Linux support.. macOS and FreeBSD would be nice, but not especially required)
* Portable (able to run from CD-Rom or flash drive) without the need to install dotnet framework
* Terminal UI & Cross-platform GUI (perhaps with Qt)
* Wizard-based interface for non-power users (such as family members)
* Parse and catalog multimedia by metadata (date, location, tags, camera info, etc)
* Ability to search entire archive for specific files or by metadata
* Think up a better name than "archiver" ;)

## History
In 2020, I got very serious about protecting my data. I do not like the 
idea of storing my data on a cloud provider, and I also do not feel 
comfortable only having my backups be online, "hot" copies. I wanted some
way to backup to something that would last for a long time, be reasonbly 
priced and be able to be stored in a fire-proof safe.

After much research, I started out by purchasing a LTO-4 tape drive. At the
time, the LTO-4 was the most cost effective tape solution and I was impressed
by the storage density, read/write performance, expected data retention
period, and the price of the media. 

From the beginning, my one main requirement would be that whatever data I put
on these tapes be able to be read by a different system in 10+ years without
the need for some special software. All the commercial options were immediately
ruled out because of the ned for that specific software. Naturally, I landed on
using `GNU tar` because of the simplicity as well as the fact that it's 
available on just about all systems and that it is just about the most "standard"
tool there is for writing tapes.

This worked well when writing tapes on Linux, but I quickly found that I wanted to
do more than just write the tape. I wanted some way to verify the tape in the future
to ensure the data integrity after it has been in the safe for a period of time.

My simple shell command suddenly got much more complex with the need to `tee` and 
`md5sum` the tar while it was in flight to the tape. I then realized that I also 
wanted to easily keep some basic information on record about each file that was placed
on a tape. File name, size, original location, create/modify date, individual checksum, 
etc. 

I also realized that there was a need for simplicity for reading/writing these tapes. 
What if something happened to me and one of my family members needed to be able to
pull cherished family photos and videos from tape. I decided that I needed to build a 
tool that others could use. 

Since I prefer Linux on my desktop, and everyone else I know uses Windows, I needed 
to make it cross platform.

... as if all of this isn't enough, I then realized that, regardless of how simple I 
make this application, it will always be easier to pull data from disc. I then stumbled 
on M-Disc and decided that for the data that is truly irreplacable, I would write a copy 
to M-Disc BluRay discs that could also be stored in the safe.

... and finally, I realized that I have data that is replaceable but I still prefer to 
have an extra copy kept offline for safe keeping. I found myself with a box of 1TB 
SATA drives that could easily be used for additional storage.