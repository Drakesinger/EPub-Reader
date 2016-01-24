# EPub-Reader
ePub reader project for .Net class.

## Features

### Main features
- Choose layout
  - [ ] Single Page
  - See Issues section
  - [x] Continous
- Choose font - Will not be implemented, check Known Issues.
  - [ ] Size
  - [ ] Character Set

### Optional features
- [ ] Bookmarks 
   - Technically can be added but cannot see where.
- [x] Table of Contents
- [ ] Highlights 
   - Same problem as with bookmarks.
- [ ] Notes
   - Same problem as with bookmarks.
- [x] Night Mode
   - See [Known Issues] section

### Known issues
 - Changing to night mode will interrupt your reading and you will be on the top of the page.
 - Changing font size will also take you to the top of the page, the Issues section describes the problem itself.
 
### Issues
Sadly, the web browser control component is limited in it's functionality towards the user.
3rd-party documentation is sparse as well.
In order to add bookmars, highlights and notes, one needs to know where in the HTML file one is located.
This information could be provided by the controler who looks at the coordinates in the view. The webbrowser control doesn't give this information, without it, I can search using regular expressions, but let's be honest, there may be a lot of matches.

Tried using [Awesomium HTML UI Engine](www.awesomium.com) however it's SDK installation proved to mess up with the Windows Explorer process and was dropped.

Have not found any way to figure out how to retrive the current view position from the web browser controller, as long as this information is not available, cannot implement more features without writing my own controller & view.
