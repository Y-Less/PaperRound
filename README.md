# PaperRound
Program to generate multi-monitor wallpapers

![PaperRound screenshot](https://raw.githubusercontent.com/Y-Less/PaperRound/73b6a2856fd589f9848608276960bec3d2d5ac2d/PaperRound.png)

This is a simple program to stitch several images together to form a single large image suitable for use as a tiled wallpaper in multi-monitor setups.  The program shows a visual representation of your monitor layout (shown by the four images in the screenshot above).  Each of these monitors can have a wallpaper assigned with the "..." button.  The image can be dragged about, or zoomed in and out with the "+" and "-" buttons (faster zooming is done with the "++" and "--" buttons).

The "Save" and "Save As" buttons save the layout independent of monitor arrangement, so if you decide to shuffle your monitors about you can reload the saved `.wallpaper` file and all the images will be correct relative to their monitor even if the monitors have moved relative to each other.

The "Export" button saves the current arrangement as a `.png` file.  This correctly handles wrapping and tiling - if your primary monitor is not the top-left monitor, the resulting wallpaper may appear sliced oddly to wrap around correctly, but (should) render correctly on the desktop.

The PaperRound/bin/Release directory has a compiled version of the program, but feel free to build your own.
