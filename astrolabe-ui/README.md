# Astrolabe-UI

The purpose of this library is to create a template style UI component library which can **copied** into a new project and customised.

## Design goals

* Have minimal dependencies that bleed through to the user of the library
  * Try to design the components to be usable outside of the DOM/CSS context. At most expose styling via className related properties.
  * Don't expose the underlying primitives (e.g. Radix)
* Every component should have a 90% use case which the API is optimised for.
  * Provide an "escape hatch" for the other 10% if it doesn't pollute the API. Remember that the library is meant to be copied, so the 10% can be handled in the application specific copy. 
