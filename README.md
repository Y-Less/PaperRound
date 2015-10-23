# PaperRound
Program to generate multi-monitor wallpapers

![PaperRound screenshot](https://raw.githubusercontent.com/Y-Less/PaperRound/73b6a2856fd589f9848608276960bec3d2d5ac2d/PaperRound.png)

This is a simple program to stitch several images together to form a single large image suitable for use as a tiled wallpaper in multi-monitor setups.  The program shows a visual representation of your monitor layout (shown by the four images in the screenshot above).  Each of these monitors can have a wallpaper assigned with the "..." button.  The image can be dragged about, or zoomed in and out with the "+" and "-" buttons (faster zooming is done with the "++" and "--" buttons).

The bottom button (a fat down arrow) opens an extended menu, with several snap options.  In order: left, top, right, bottom, width, height, reset.  The first four adjust the position of an image to be flush with the screen.  The next two adjust the size to fit in one dimension of the screen (you can't do both - PaperRound is explicitly designed to preserve image ratios).  The final one resets all modifications made to the image thus far.

The "Save" and "Save As" buttons save the layout independent of monitor arrangement, so if you decide to shuffle your monitors about you can reload the saved `.wallpaper` file and all the images will be correct relative to their monitor even if the monitors have moved relative to each other.

The "Export" button saves the current arrangement as a `.png` file.  This correctly handles wrapping and tiling - if your primary monitor is not the top-left monitor, the resulting wallpaper may appear sliced oddly to wrap around correctly, but (should) render correctly on the desktop.

To use the resulting file, select it as your wallpaper, and set the mode to "tile" - this is the only mode that allows one wallpaper to stretch across multiple monitors.

The PaperRound/bin/Release directory has a compiled version of the program, but feel free to build your own.
