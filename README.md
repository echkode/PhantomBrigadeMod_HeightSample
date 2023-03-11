# HeightSample

A library mod for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/) that adds a console command to sample heights of coordinates on the overworld map.

**This is a mod for mod makers.**

It is compatible with game patch **1.0.4**. All library mods are fragile and susceptible to breakage whenever a new version is released.

This mod adds a console command named `ow.sample-coords` that lets you feed it a list of 2D x-z coordinates and it will sample the overworld map with those coordinates and return a corresponding list of 3D x-y-z coordinates. This is helpful if you want to change the spawn sites in a province or add an entirely new province.

The command looks for a file named `xz.txt` in the mod's directory. This file should contain a list of comma-separated x-z coordinates, one per line. When the command finishes, it will generate a file named `xyz.txt` in the same directory which will contain the matching x-y-z coordinates as comma-separated triples.
